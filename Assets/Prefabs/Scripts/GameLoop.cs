using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum LoopState { Starting, Fighting, KeyHunt, GateOpen, Choosing, Dead }

// Runs the game: build a random level, survive its waves, find the key, open the gate, go through,
// sometimes pick an ability, and do it all again on a new level. Dying ends the run and goes back to the menu.
// One per Demo scene; the player, its health and its inventory stay the same across levels.
public class GameLoop : MonoBehaviour
{
    [Header("Scene References (found automatically when empty)")]
    public LevelRandomizer randomizer;
    public WaveSpawner spawner;
    public AbilityChoiceScreen choiceScreen;
    public LoopHud hud;

    [Header("Prefabs")]
    public KeyPickup keyPrefab;
    public Gate gatePrefab;
    public AbilityPool abilityPool;

    [Header("Rules")]
    [Tooltip("After every this many levels, going through the gate offers a choice of abilities. 0 = never.")]
    public int abilityEveryNLevels = 2;
    [Tooltip("Cards offered on the choice screen.")]
    public int choiceCount = 3;
    [Tooltip("Seconds the 'Level N' banner shows before the first wave.")]
    public float levelBannerTime = 2f;
    [Tooltip("Real seconds 'You died' stays up before going back to the menu.")]
    public float deathScreenTime = 1.5f;
    [Tooltip("The key never appears closer than this to the player.")]
    public float minKeyDistance = 4f;
    [Tooltip("Scene loaded after dying. It has to be in the Build Settings scene list.")]
    public string menuSceneName = "Start Menu";

    public int Level { get; private set; }
    public LoopState State { get; private set; } = LoopState.Starting;
    public Gate CurrentGate => gate;

    // The pause menu stays shut while a choice or the death screen is up
    public static bool BlocksPause { get; private set; }

    public event Action<LoopState> StateChanged;

    private Transform player;
    private Health playerHealth;
    private Inventory inventory;
    private KeyPickup key;
    private Gate gate;
    private Coroutine levelRoutine;
    private LoopState stateBeforeTestChoice;
    private readonly System.Random rng = new System.Random();

    // The whole state machine. Anything not listed leaves the state as it is; nothing leaves Dead
    public static LoopState Transition(LoopState from, string evt)
    {
        if (from == LoopState.Dead) return LoopState.Dead;
        if (evt == "died") return LoopState.Dead;

        switch (evt)
        {
            case "levelReady": return from == LoopState.Starting ? LoopState.Fighting : from;
            case "wavesCleared": return from == LoopState.Fighting ? LoopState.KeyHunt : from;
            // The Test Menu can open the gate before the waves are done
            case "gateOpened": return from == LoopState.KeyHunt || from == LoopState.Fighting ? LoopState.GateOpen : from;
            case "choose": return from == LoopState.GateOpen ? LoopState.Choosing : from;
            case "gateEntered": return from == LoopState.GateOpen ? LoopState.Starting : from;
            case "choiceMade": return from == LoopState.Choosing ? LoopState.Starting : from;
            case "nextLevel": return LoopState.Starting;
            default: return from;
        }
    }

    public static bool OffersChoice(int level, int everyN)
    {
        return everyN > 0 && level % everyN == 0;
    }

    // The gate may only be forced open while a level is being played. Opened during the banner or
    // after it is already open, the loop would ignore it and the gate would never let the player through
    public static bool CanForceOpenGate(LoopState state)
    {
        return state == LoopState.Fighting || state == LoopState.KeyHunt;
    }

    // The spawner keeps counting while a test choice has the game frozen, so the last wave can end
    // unseen. Back in Fighting with the waves finished, the key is still owed
    public static bool WavesFinishedWhileAway(LoopState state, bool spawnerRunning, int totalWaves)
    {
        return state == LoopState.Fighting && !spawnerRunning && totalWaves > 0;
    }

    void Awake()
    {
        if (randomizer == null) randomizer = FindAnyObjectByType<LevelRandomizer>();
        if (spawner == null) spawner = FindAnyObjectByType<WaveSpawner>();
        if (choiceScreen == null) choiceScreen = FindAnyObjectByType<AbilityChoiceScreen>(FindObjectsInactive.Include);
        if (hud == null) hud = FindAnyObjectByType<LoopHud>();
        BlocksPause = false;
    }

    void Start()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.transform;
            playerHealth = playerObject.GetComponent<Health>();
            inventory = playerObject.GetComponent<Inventory>();
        }
        if (playerHealth != null) playerHealth.Died += OnPlayerDied;
        if (spawner != null) spawner.AllWavesCleared += OnWavesCleared;

        BeginLevel();
    }

    void OnDestroy()
    {
        if (playerHealth != null) playerHealth.Died -= OnPlayerDied;
        if (spawner != null) spawner.AllWavesCleared -= OnWavesCleared;
        BlocksPause = false;
        // Time scale survives scene changes, so never leave the next scene frozen
        Time.timeScale = 1f;
    }

    void Apply(string evt)
    {
        LoopState next = Transition(State, evt);
        if (next == State) return;
        State = next;
        BlocksPause = State == LoopState.Choosing || State == LoopState.Dead;
        StateChanged?.Invoke(State);
    }

    // ---------- Levels ----------

    public void BeginLevel()
    {
        if (State == LoopState.Dead) return;
        if (levelRoutine != null) StopCoroutine(levelRoutine);
        levelRoutine = StartCoroutine(LevelRoutine());
    }

    IEnumerator LevelRoutine()
    {
        Level++;
        Apply("nextLevel");

        // Clear the old level first: enemies left alive could end up inside the new walls,
        // and their pathfinders would remember the old ones
        if (spawner != null) spawner.StopAll();
        if (key != null) Destroy(key.gameObject);
        if (gate != null) Destroy(gate.gameObject);
        key = null;
        gate = null;

        randomizer.Randomize();
        PlaceGate();

        if (hud != null) hud.ShowBanner("Level " + Level, levelBannerTime);
        yield return new WaitForSeconds(levelBannerTime);

        if (State == LoopState.Dead) yield break;
        if (spawner != null) spawner.StartLevel(Level);
        Apply("levelReady");
        levelRoutine = null;
    }

    // In the room the most doors away from the start, so the whole level lies between them
    void PlaceGate()
    {
        if (gatePrefab == null) return;

        int room = LevelGraph.FarthestRoom(randomizer.Rooms.Count, randomizer.RoomLinks, randomizer.Rooms, randomizer.StartRoomIndex);
        Vector2 spot = randomizer.TryGetRandomFloorPoint(room, out Vector2 point) ? point : randomizer.RoomCenter(room);

        gate = Instantiate(gatePrefab, new Vector3(spot.x, spot.y, PlayerZ()), Quaternion.identity);
        gate.Opened += () => Apply("gateOpened");
        gate.Entered += OnGateEntered;
        gate.NeedsKey += () => { if (hud != null) hud.ShowNeedKey(); };
    }

    void OnWavesCleared()
    {
        if (State != LoopState.Fighting) return;
        SpawnKey();
        Apply("wavesCleared");
    }

    // Somewhere the player is not: a random room among the farther half from the player's room.
    // In a one-room level it goes in the same room, at least minKeyDistance away
    void SpawnKey()
    {
        if (keyPrefab == null || key != null) return;

        int playerRoom = Mathf.Max(0, randomizer.RoomIndexAt(player.position));
        List<int> rooms = LevelGraph.RoomsByDistance(randomizer.Rooms.Count, randomizer.RoomLinks, randomizer.Rooms, playerRoom);

        Vector2? spot = null;
        if (rooms.Count > 0)
        {
            int farHalf = Mathf.Max(1, (rooms.Count + 1) / 2);
            for (int attempt = 0; attempt < 10 && spot == null; attempt++)
            {
                int room = rooms[UnityEngine.Random.Range(0, farHalf)];
                if (randomizer.TryGetRandomFloorPoint(room, out Vector2 point) && !OnGate(point)) spot = point;
            }
        }
        for (int attempt = 0; attempt < 20 && spot == null; attempt++)
        {
            if (randomizer.TryGetRandomFloorPoint(playerRoom, out Vector2 point)
                && Vector2.Distance(point, player.position) >= minKeyDistance && !OnGate(point))
            {
                spot = point;
            }
        }
        // Last resort: right where the player is, so the loop can never get stuck without a key
        Vector2 at = spot ?? (Vector2)player.position;

        key = Instantiate(keyPrefab, new Vector3(at.x, at.y, PlayerZ()), Quaternion.identity);
    }

    bool OnGate(Vector2 point)
    {
        return gate != null && Vector2.Distance(point, gate.transform.position) < 2f;
    }

    void OnGateEntered()
    {
        if (State != LoopState.GateOpen) return;

        if (OffersChoice(Level, abilityEveryNLevels) && choiceScreen != null && abilityPool != null && inventory != null)
        {
            List<Ability> cards = AbilityPool.Draw(abilityPool.abilities, inventory.StackCount, choiceCount, rng);
            if (cards.Count > 0)
            {
                Apply("choose");
                choiceScreen.Open(cards, OnAbilityPicked);
                return;
            }
        }

        Apply("gateEntered");
        BeginLevel();
    }

    void OnAbilityPicked(Ability ability)
    {
        if (State != LoopState.Choosing) return;
        inventory.AddAbility(ability);
        Apply("choiceMade");
        BeginLevel();
    }

    // ---------- Death ----------

    void OnPlayerDied()
    {
        if (State == LoopState.Dead) return;
        Apply("died");
        if (levelRoutine != null) StopCoroutine(levelRoutine);
        if (choiceScreen != null) choiceScreen.Close();
        StartCoroutine(DeathRoutine());
    }

    IEnumerator DeathRoutine()
    {
        Time.timeScale = 0f;
        if (hud != null) hud.ShowDeath();
        yield return new WaitForSecondsRealtime(deathScreenTime);
        Time.timeScale = 1f;
        SceneManager.LoadScene(menuSceneName);
    }

    float PlayerZ()
    {
        return player != null ? player.position.z : 0f;
    }

    // ---------- Test Menu ----------

    // Puts the key right next to the player
    public void ForceKey()
    {
        if (keyPrefab == null || player == null || State == LoopState.Dead) return;
        if (key != null) Destroy(key.gameObject);
        Vector3 at = player.position + Vector3.right * 1.5f;
        key = Instantiate(keyPrefab, at, Quaternion.identity);
    }

    public void ForceOpenGate()
    {
        if (gate != null && CanForceOpenGate(State)) gate.Open();
    }

    // Moves the player to the gate, opening it, so the next step is going through
    public void TeleportToGate()
    {
        if (gate == null || player == null) return;
        Rigidbody2D body = player.GetComponent<Rigidbody2D>();
        if (body != null) body.position = gate.transform.position;
        else player.position = gate.transform.position;
    }

    public void NextLevel()
    {
        if (State == LoopState.Dead || State == LoopState.Choosing) return;
        BeginLevel();
    }

    // Shows the choice screen without finishing the level; afterwards the level carries on where it was
    public void OfferChoiceNow()
    {
        if (State == LoopState.Dead || State == LoopState.Choosing || choiceScreen == null || abilityPool == null || inventory == null) return;

        List<Ability> cards = AbilityPool.Draw(abilityPool.abilities, inventory.StackCount, choiceCount, rng);
        if (cards.Count == 0) return;

        stateBeforeTestChoice = State;
        State = LoopState.Choosing;
        BlocksPause = true;
        StateChanged?.Invoke(State);
        choiceScreen.Open(cards, ability =>
        {
            if (State != LoopState.Choosing) return;
            inventory.AddAbility(ability);
            State = stateBeforeTestChoice;
            BlocksPause = false;
            StateChanged?.Invoke(State);
            if (spawner != null && WavesFinishedWhileAway(State, spawner.Running, spawner.TotalWaves)) OnWavesCleared();
        });
    }
}
