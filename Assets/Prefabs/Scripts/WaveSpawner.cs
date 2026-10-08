using System;
using System.Collections.Generic;
using UnityEngine;

// Sends waves of enemies at the player in a generated level. Each wave spawns in the room the player is in,
// on random floor spots away from the player and not bunched together. A glowing marker shows each spot
// for a moment before the enemy appears. A wave is over when no enemy is left alive (babies count too);
// after a short breather the next one comes, and after the last one AllWavesCleared fires.
public class WaveSpawner : MonoBehaviour
{
    [Header("References (found automatically when empty)")]
    public LevelRandomizer randomizer;
    public Transform player;
    [Tooltip("What each wave is made of.")]
    public GameObject enemyPrefab;
    [Tooltip("Shown on each spawn spot before the enemy appears.")]
    public GameObject spawnMarkerPrefab;

    [Header("Warning before a wave")]
    [Tooltip("Seconds the spawn markers glow before the enemies appear.")]
    public float telegraphTime = 1.5f;

    [Header("Waves per level")]
    public int baseWaves = 2;
    [Tooltip("Extra waves per level, rounded down (0.5 = one more every second level).")]
    public float wavesPerLevel = 0.5f;
    public int maxWaves = 6;

    [Header("Enemies per wave")]
    public int baseEnemiesPerWave = 2;
    public int enemiesPerLevel = 1;
    public int maxEnemiesPerWave = 10;
    [Tooltip("Seconds between clearing a wave and the next one starting.")]
    public float timeBetweenWaves = 3f;

    [Header("Where enemies may spawn")]
    [Tooltip("No enemy spawns closer than this to the player.")]
    public float minPlayerDistance = 4f;
    [Tooltip("No enemy spawns closer than this to another living enemy.")]
    public float minEnemySpacing = 2f;
    [Tooltip("At most maxPerRadius living enemies may be within this distance of a new spawn.")]
    public float crowdRadius = 5f;
    public int maxPerRadius = 3;
    [Tooltip("Random spots tried per enemy before giving up on it. A wave may come up short in a small level.")]
    public int spawnAttemptsPerEnemy = 15;
    [Tooltip("Chance for each enemy to spawn in a random other room instead of the player's. Enemies in rooms the player hasn't explored can't see them.")]
    [Range(0f, 1f)] public float otherRoomChance = 0.5f;

    public int CurrentWave { get; private set; }
    public int TotalWaves { get; private set; }
    public bool Running { get; private set; }
    // Seconds until the next wave, 0 while a wave is coming or being fought
    public float NextWaveIn => Running && !waveActive && !telegraphing ? Mathf.Max(0f, nextWaveTime - Time.time) : 0f;
    public bool Telegraphing => telegraphing;

    public event Action<int> WaveStarted;
    public event Action AllWavesCleared;

    private bool waveActive;
    private float nextWaveTime;
    private int level;
    private Transform enemiesParent;
    private bool telegraphing;
    private float telegraphUntil;
    private readonly List<GameObject> markers = new List<GameObject>();
    private readonly List<Vector2> pendingSpawns = new List<Vector2>();

    public static int WaveCount(int level, int baseWaves, float wavesPerLevel, int maxWaves)
    {
        return Mathf.Min(maxWaves, baseWaves + Mathf.FloorToInt(level * wavesPerLevel));
    }

    public static int WaveSize(int level, int baseEnemies, int enemiesPerLevel, int maxEnemies)
    {
        return Mathf.Min(maxEnemies, baseEnemies + level * enemiesPerLevel);
    }

    // Waves come where the player is. Outside every room (between rooms): the start room
    public static int SpawnRoom(int playerRoom, int startRoom)
    {
        return playerRoom >= 0 ? playerRoom : startRoom;
    }

    public static bool IsValidSpawn(Vector2 point, Vector2 player, IList<Vector2> enemies,
        float minPlayerDistance, float minEnemySpacing, float crowdRadius, int maxPerRadius)
    {
        if (Vector2.Distance(point, player) < minPlayerDistance) return false;

        int crowd = 0;
        foreach (Vector2 enemy in enemies)
        {
            float distance = Vector2.Distance(point, enemy);
            if (distance < minEnemySpacing) return false;
            if (distance <= crowdRadius) crowd++;
        }
        return crowd < maxPerRadius;
    }

    // Draws spots from 'sample' until one passes 'valid', at most 'attempts' times. A null sample counts as a miss
    public static bool TryFindSpawn(Func<Vector2?> sample, Func<Vector2, bool> valid, int attempts, out Vector2 point)
    {
        for (int i = 0; i < attempts; i++)
        {
            Vector2? candidate = sample();
            if (candidate.HasValue && valid(candidate.Value))
            {
                point = candidate.Value;
                return true;
            }
        }
        point = Vector2.zero;
        return false;
    }

    void Awake()
    {
        if (randomizer == null) randomizer = FindAnyObjectByType<LevelRandomizer>();
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null) player = playerObject.transform;
        }
    }

    public void StartLevel(int level)
    {
        this.level = level;
        TotalWaves = WaveCount(level, baseWaves, wavesPerLevel, maxWaves);
        CurrentWave = 0;
        Running = true;
        waveActive = false;
        nextWaveTime = Time.time;
    }

    // Stops the waves and removes every enemy, without big slimes splitting or anything being reported
    public void StopAll()
    {
        Running = false;
        waveActive = false;
        ClearMarkers();
        foreach (EnemyHealth enemy in new List<EnemyHealth>(EnemyHealth.Alive)) Destroy(enemy.gameObject);
    }

    // Removes the living enemies and any wave that is about to appear; the wave then counts as cleared as usual
    public void KillAll()
    {
        if (telegraphing)
        {
            ClearMarkers();
            waveActive = true;
        }
        foreach (EnemyHealth enemy in new List<EnemyHealth>(EnemyHealth.Alive)) Destroy(enemy.gameObject);
    }

    void ClearMarkers()
    {
        telegraphing = false;
        pendingSpawns.Clear();
        foreach (GameObject marker in markers) if (marker != null) Destroy(marker);
        markers.Clear();
    }

    // Ends the current wave now: removes its enemies and starts the next one without the breather
    public void SkipWave()
    {
        if (!Running) return;
        KillAll();
        nextWaveTime = Time.time;
    }

    void Update()
    {
        if (!Running) return;

        if (telegraphing)
        {
            if (Time.time >= telegraphUntil) SpawnPending();
            return;
        }

        if (waveActive)
        {
            // Checked here rather than on death events, so a split or merge in progress is never mistaken for a clear
            if (EnemyHealth.AliveCount > 0) return;

            waveActive = false;
            if (CurrentWave >= TotalWaves)
            {
                Running = false;
                AllWavesCleared?.Invoke();
                return;
            }
            nextWaveTime = Time.time + timeBetweenWaves;
            return;
        }

        if (Time.time >= nextWaveTime) SpawnWave();
    }

    // Picks the spots and puts a glowing marker on each; the enemies follow after telegraphTime
    void SpawnWave()
    {
        CurrentWave++;

        int size = WaveSize(level, baseEnemiesPerWave, enemiesPerLevel, maxEnemiesPerWave);
        pendingSpawns.Clear();
        if (enemyPrefab != null && randomizer != null && randomizer.Rooms.Count > 0 && player != null)
        {
            var taken = new List<Vector2>();
            foreach (EnemyHealth enemy in EnemyHealth.Alive) taken.Add(enemy.transform.position);

            for (int i = 0; i < size; i++)
            {
                if (!TryFindSpawn(SampleFloorPoint, p => IsValidSpawn(p, player.position, taken,
                        minPlayerDistance, minEnemySpacing, crowdRadius, maxPerRadius), spawnAttemptsPerEnemy, out Vector2 point))
                {
                    continue;
                }
                taken.Add(point);
                pendingSpawns.Add(point);
            }
        }

        if (pendingSpawns.Count < size) Debug.Log("Wave " + CurrentWave + ": " + pendingSpawns.Count + " of " + size + " enemies fit in the room with the spacing rules.");
        WaveStarted?.Invoke(CurrentWave);

        // Nothing fits: the wave counts as cleared on the next frame instead of stalling the level
        if (pendingSpawns.Count == 0)
        {
            waveActive = true;
            return;
        }

        foreach (Vector2 point in pendingSpawns)
        {
            if (spawnMarkerPrefab != null)
                markers.Add(Instantiate(spawnMarkerPrefab, new Vector3(point.x, point.y, player.position.z), Quaternion.identity));
        }
        telegraphing = true;
        telegraphUntil = Time.time + telegraphTime;
    }

    void SpawnPending()
    {
        if (enemiesParent == null) enemiesParent = new GameObject("Enemies").transform;
        float z = player != null ? player.position.z : 0f;
        foreach (Vector2 point in pendingSpawns)
        {
            Instantiate(enemyPrefab, new Vector3(point.x, point.y, z), Quaternion.identity, enemiesParent);
        }
        ClearMarkers();
        waveActive = true;
    }

    Vector2? SampleFloorPoint()
    {
        int room = SpawnRoom(randomizer.RoomIndexAt(player.position), randomizer.StartRoomIndex);

        // Sometimes somewhere else in the level, so the player has to go looking
        int roomCount = randomizer.Rooms.Count;
        if (roomCount > 1 && UnityEngine.Random.value < otherRoomChance)
        {
            int other = UnityEngine.Random.Range(0, roomCount - 1);
            room = other >= room ? other + 1 : other;
        }

        return randomizer.TryGetRandomFloorPoint(room, out Vector2 point) ? point : (Vector2?)null;
    }
}
