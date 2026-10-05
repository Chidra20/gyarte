using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Developer test menu: one window for trying out everything the game has so far without leaving Play mode.
// Separate from the pause menu and does not pause the game.
// Every new system gets a section here: write a Draw...() method for it and call it from DrawWindow().
public class TestMenu : MonoBehaviour
{
    [System.Serializable]
    public class Spawnable
    {
        public string name;
        public GameObject prefab;
    }

    private enum ClickMode { Nothing, SpawnEnemy, MovePlayer }

    [Header("Menu")]
    [Tooltip("Key that shows and hides the test menu.")]
    public Key toggleKey = Key.U;
    public bool openOnStart = false;
    [Tooltip("The menu is drawn as if the screen were this many pixels tall, so it looks the same at any resolution. Smaller values make it bigger.")]
    public float referenceHeight = 720f;

    [Header("Enemies")]
    [Tooltip("Everything the menu can spawn. Add new enemy prefabs here.")]
    public Spawnable[] spawnables;
    [Tooltip("Enemies are not spawned on top of colliders with this tag.")]
    public string wallTag = "wall";

    [Header("Scene References (found automatically when left empty)")]
    public PlayerMovement player;
    public LevelRandomizer levelRandomizer;
    public RoomManager roomManager;

    public bool IsOpen { get; private set; }

    private const int WindowId = 7341;
    private const float LabelWidth = 150f;

    private PlayerAttack attack;
    private Health playerHealth;
    private Rect windowRect = new Rect(10f, 10f, 350f, 600f);
    private Vector2 scroll;
    private GUIStyle headerStyle;
    private string status = "";

    private int selectedSpawnable;
    private int spawnHealth = 2;
    private int spawnCount = 1;
    private float spawnDistance = 3f;
    private ClickMode clickMode = ClickMode.Nothing;
    private int enemyCount;

    private bool freezeEnemies;
    private bool unlimitedSpell;
    private bool showAllRooms;

    // Applied to every living enemy and to each one spawned from the menu
    private int contactDamage = 1;
    private float contactInterval = 1f;
    private float contactKnockback = 8f;

    void Start()
    {
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null) player = playerObject.GetComponent<PlayerMovement>();
        }
        if (player != null)
        {
            attack = player.GetComponent<PlayerAttack>();
            playerHealth = player.GetComponent<Health>();
        }
        if (levelRandomizer == null) levelRandomizer = FindAnyObjectByType<LevelRandomizer>();
        if (roomManager == null) roomManager = FindAnyObjectByType<RoomManager>();

        IsOpen = openOnStart;
        SelectSpawnable(0);

        // Start the sliders at what the prefab normally does
        ContactDamage prefabContact = spawnables != null && spawnables.Length > 0 && spawnables[0].prefab != null
            ? spawnables[0].prefab.GetComponent<ContactDamage>() : null;
        if (prefabContact != null)
        {
            contactDamage = prefabContact.damage;
            contactInterval = prefabContact.damageInterval;
            contactKnockback = prefabContact.knockbackSpeed;
        }
    }

    void Update()
    {
        if (toggleKey != Key.None && Keyboard.current != null && Keyboard.current[toggleKey].wasPressedThisFrame)
        {
            Toggle();
        }

        // These keep working while the window is hidden, so a test setup survives closing the menu
        if (unlimitedSpell && attack != null) attack.RefillSpellCharges();
        if (freezeEnemies) SetEnemiesFrozen(true);
        if (showAllRooms) HideDarkness();

        if (!IsOpen) return;

        // The same count the wave spawner uses to decide a wave is cleared
        enemyCount = EnemyHealth.AliveCount;
        HandleMapClick();
    }

    public void Toggle()
    {
        IsOpen = !IsOpen;
    }

    // ---------- Window ----------

    void OnGUI()
    {
        if (!IsOpen) return;

        float scale = GuiScale();
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));

        if (headerStyle == null)
        {
            headerStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };

            // Start on the right, clear of the game's own buttons in the top left corner
            windowRect.x = Screen.width / scale - windowRect.width - 10f;
        }

        windowRect.height = Mathf.Min(600f, Screen.height / scale - 20f);

        // The default window is see-through; a dark backing keeps it readable over the level
        Color previous = GUI.color;
        GUI.color = new Color(0.08f, 0.08f, 0.1f, 0.92f);
        GUI.DrawTexture(windowRect, Texture2D.whiteTexture);
        GUI.color = previous;

        windowRect = GUI.Window(WindowId, windowRect, DrawWindow, "Test Menu (" + toggleKey + " to hide)");
    }

    float GuiScale()
    {
        return Mathf.Max(0.1f, Screen.height / Mathf.Max(1f, referenceHeight));
    }

    void DrawWindow(int id)
    {
        scroll = GUILayout.BeginScrollView(scroll);

        DrawEnemies();
        DrawPlayer();
        DrawLevel();
        DrawGame();

        GUILayout.EndScrollView();

        GUILayout.Label(status);

        // The title bar moves the window
        GUI.DragWindow(new Rect(0f, 0f, 10000f, 20f));
    }

    // ---------- Enemies ----------

    void DrawEnemies()
    {
        Header("Enemies (alive: " + enemyCount + ")");

        if (spawnables == null || spawnables.Length == 0)
        {
            GUILayout.Label("No enemy prefabs set on the Test Menu object.");
        }
        else
        {
            string[] names = new string[spawnables.Length];
            for (int i = 0; i < spawnables.Length; i++) names[i] = spawnables[i].name;

            int picked = GUILayout.SelectionGrid(selectedSpawnable, names, 2);
            if (picked != selectedSpawnable) SelectSpawnable(picked);

            IntSlider("Health", ref spawnHealth, 1, 50);
            IntSlider("How many", ref spawnCount, 1, 10);
            Slider("Distance in front", ref spawnDistance, 1f, 8f);

            if (GUILayout.Button("Spawn in front of player")) SpawnInFrontOfPlayer();
        }

        GUILayout.Label("Left click on the map:");
        clickMode = (ClickMode)GUILayout.SelectionGrid((int)clickMode, new[] { "Nothing", "Spawn enemy", "Move player" }, 3);

        bool freeze = GUILayout.Toggle(freezeEnemies, " Freeze enemies");
        if (freeze != freezeEnemies)
        {
            freezeEnemies = freeze;
            SetEnemiesFrozen(freeze);
        }

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Hit all once")) HitAllEnemies();
        if (GUILayout.Button("Remove all")) RemoveAllEnemies();
        GUILayout.EndHorizontal();

        GUILayout.Label("Touching the player:");
        int oldDamage = contactDamage;
        float oldInterval = contactInterval, oldKnockback = contactKnockback;
        IntSlider("Contact damage", ref contactDamage, 0, 10);
        Slider("Seconds between hits", ref contactInterval, 0.1f, 5f);
        Slider("Knockback speed", ref contactKnockback, 0f, 20f);
        if (oldDamage != contactDamage || oldInterval != contactInterval || oldKnockback != contactKnockback)
        {
            foreach (EnemyHealth enemy in EnemyHealth.Alive) ApplyContactSettings(enemy.gameObject);
        }
    }

    void ApplyContactSettings(GameObject enemy)
    {
        ContactDamage contact = enemy.GetComponent<ContactDamage>();
        if (contact == null) return;
        contact.damage = contactDamage;
        contact.damageInterval = contactInterval;
        contact.knockbackSpeed = contactKnockback;
    }

    // Start the health slider at whatever the chosen prefab normally has
    void SelectSpawnable(int index)
    {
        selectedSpawnable = index;
        if (spawnables == null || index < 0 || index >= spawnables.Length || spawnables[index].prefab == null) return;

        EnemyHealth health = spawnables[index].prefab.GetComponent<EnemyHealth>();
        if (health != null) spawnHealth = health.maxHealth;
    }

    void SpawnInFrontOfPlayer()
    {
        if (player == null)
        {
            status = "No player in this scene.";
            return;
        }

        SpawnEnemy((Vector2)player.transform.position + player.FacingVector * spawnDistance);
    }

    // Spawns the chosen enemy (as many as "How many" says) at a spot in the world, with the chosen health
    public void SpawnEnemy(Vector2 position)
    {
        if (spawnables == null || selectedSpawnable >= spawnables.Length || spawnables[selectedSpawnable].prefab == null)
        {
            status = "No enemy prefab to spawn.";
            return;
        }
        if (InsideWall(position))
        {
            status = "That spot is inside a wall.";
            return;
        }

        Spawnable spawnable = spawnables[selectedSpawnable];
        float z = player != null ? player.transform.position.z : 0f;

        for (int i = 0; i < spawnCount; i++)
        {
            // Spread a group out a little so they don't all sit on the same point
            Vector2 spot = position;
            if (i > 0)
            {
                Vector2 nearby = position + Random.insideUnitCircle * 0.75f;
                if (!InsideWall(nearby)) spot = nearby;
            }

            GameObject enemy = Instantiate(spawnable.prefab, new Vector3(spot.x, spot.y, z), Quaternion.identity);
            EnemyHealth health = enemy.GetComponent<EnemyHealth>();
            if (health != null) health.SetMaxHealth(spawnHealth);
            ApplyContactSettings(enemy);
        }

        status = "Spawned " + spawnCount + " x " + spawnable.name + " with " + spawnHealth + " health.";
    }

    bool InsideWall(Vector2 position)
    {
        foreach (Collider2D hit in Physics2D.OverlapPointAll(position))
        {
            if (!hit.isTrigger && hit.CompareTag(wallTag)) return true;
        }
        return false;
    }

    void SetEnemiesFrozen(bool frozen)
    {
        foreach (BigSlime slime in FindObjectsByType<BigSlime>(FindObjectsInactive.Exclude)) slime.enabled = !frozen;
        foreach (BabySlime slime in FindObjectsByType<BabySlime>(FindObjectsInactive.Exclude)) slime.enabled = !frozen;
    }

    void HitAllEnemies()
    {
        int damage = attack != null ? attack.attackDamage : 1;
        EnemyHealth[] enemies = FindObjectsByType<EnemyHealth>(FindObjectsInactive.Exclude);
        foreach (EnemyHealth enemy in enemies) enemy.TakeDamage(damage);

        status = "Hit " + enemies.Length + " enemies for " + damage + ".";
    }

    // Removes every enemy outright, without big slimes splitting
    public void RemoveAllEnemies()
    {
        EnemyHealth[] enemies = FindObjectsByType<EnemyHealth>(FindObjectsInactive.Exclude);
        foreach (EnemyHealth enemy in enemies) Destroy(enemy.gameObject);

        status = "Removed " + enemies.Length + " enemies.";
    }

    // ---------- Mouse ----------

    void HandleMapClick()
    {
        if (clickMode == ClickMode.Nothing) return;
        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame) return;

        Camera cam = Camera.main;
        if (cam == null) return;

        // Clicks on this window or on the normal UI are not clicks on the map
        Vector2 screenPoint = Mouse.current.position.ReadValue();
        float scale = GuiScale();
        Vector2 guiPoint = new Vector2(screenPoint.x / scale, (Screen.height - screenPoint.y) / scale);
        if (windowRect.Contains(guiPoint)) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        float planeZ = player != null ? player.transform.position.z : 0f;
        float depth = Mathf.Abs(cam.transform.position.z - planeZ);
        Vector2 worldPoint = cam.ScreenToWorldPoint(new Vector3(screenPoint.x, screenPoint.y, depth));

        if (clickMode == ClickMode.SpawnEnemy) SpawnEnemy(worldPoint);
        else MovePlayer(worldPoint);
    }

    void MovePlayer(Vector2 position)
    {
        if (player == null) return;

        player.transform.position = new Vector3(position.x, position.y, player.transform.position.z);

        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.position = position;
            rb.linearVelocity = Vector2.zero;
        }

        status = "Moved the player.";
    }

    // ---------- Player ----------

    void DrawPlayer()
    {
        Header("Player");

        if (player == null)
        {
            GUILayout.Label("No player in this scene.");
            return;
        }

        Slider("Move speed", ref player.moveSpeed, 1f, 15f);

        if (playerHealth != null)
        {
            GUILayout.Label("Health: " + playerHealth.CurrentHealth + " / " + playerHealth.maxHealth + (playerHealth.IsDead ? "  (dead)" : ""));

            int max = playerHealth.maxHealth;
            IntSlider("Max health", ref max, 1, 50);
            if (max != playerHealth.maxHealth) playerHealth.SetMaxHealth(max, false);

            int current = playerHealth.CurrentHealth;
            IntSlider("Health", ref current, 1, playerHealth.maxHealth);
            if (current != playerHealth.CurrentHealth) SetPlayerHealth(current);

            Slider("Hurt immunity seconds", ref playerHealth.invulnerableTime, 0f, 3f);
            playerHealth.GodMode = GUILayout.Toggle(playerHealth.GodMode, " God mode (no damage)");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Heal to full")) SetPlayerHealth(playerHealth.maxHealth);
            if (GUILayout.Button("Take 1 damage")) playerHealth.TakeDamage(1);
            GUILayout.EndHorizontal();
        }

        if (attack == null) return;

        IntSlider("Attack damage", ref attack.attackDamage, 1, 20);
        Slider("Swing range", ref attack.attackRange, 0.25f, 3f);
        Slider("Swing cooldown", ref attack.meleeCooldown, 0f, 3f);
        attack.showHitbox = GUILayout.Toggle(attack.showHitbox, " Show swing hitbox");

        GUILayout.Label("Fire spell charges: " + attack.SpellCharges + " / " + attack.maxSpellCharges);
        IntSlider("Max charges", ref attack.maxSpellCharges, 1, 10);
        Slider("Recharge seconds", ref attack.spellRechargeTime, 0.5f, 15f);
        unlimitedSpell = GUILayout.Toggle(unlimitedSpell, " Unlimited fire spell");
        attack.lockMovementWhileCasting = GUILayout.Toggle(attack.lockMovementWhileCasting, " Stand still while casting");

        if (GUILayout.Button("Refill fire spell")) attack.RefillSpellCharges();
    }

    void SetPlayerHealth(int value)
    {
        if (playerHealth != null) playerHealth.SetCurrentHealth(value);
    }

    // ---------- Level ----------

    void DrawLevel()
    {
        Header("Level");

        if (levelRandomizer == null && roomManager == null)
        {
            GUILayout.Label("This scene has no generated level or rooms.");
            return;
        }

        if (levelRandomizer != null && GUILayout.Button("Randomize level (removes enemies)"))
        {
            // Old enemies could end up inside the new walls, and their pathfinders remember the old ones
            RemoveAllEnemies();
            levelRandomizer.Randomize();
            status = "New level built.";
        }

        if (roomManager != null)
        {
            bool show = GUILayout.Toggle(showAllRooms, " Show all rooms (no darkness)");
            if (show != showAllRooms)
            {
                showAllRooms = show;
                if (show) HideDarkness();
                else RestoreDarkness();
            }
        }
    }

    // RoomManager is switched off while everything is shown, or it would darken rooms again
    void HideDarkness()
    {
        if (roomManager == null) return;

        roomManager.enabled = false;
        foreach (Transform room in roomManager.transform)
        {
            SpriteRenderer overlay = room.GetComponent<SpriteRenderer>();
            if (overlay != null) overlay.enabled = false;
        }
    }

    void RestoreDarkness()
    {
        if (roomManager == null) return;

        foreach (Transform room in roomManager.transform)
        {
            SpriteRenderer overlay = room.GetComponent<SpriteRenderer>();
            BoxCollider2D box = room.GetComponent<BoxCollider2D>();
            if (overlay == null) continue;

            bool playerInside = player != null && box != null && box.OverlapPoint(player.transform.position);
            overlay.enabled = !playerInside;
        }
        roomManager.enabled = true;
    }

    // ---------- Game ----------

    void DrawGame()
    {
        Header("Game");

        if (Time.timeScale == 0f)
        {
            GUILayout.Label("Paused.");
        }
        else
        {
            // The pause menu sets the speed back to normal when it resumes
            float speed = Time.timeScale;
            Slider("Game speed", ref speed, 0.1f, 3f);
            if (!Mathf.Approximately(speed, Time.timeScale)) Time.timeScale = speed;

            if (GUILayout.Button("Normal speed")) Time.timeScale = 1f;
        }

        if (GUILayout.Button("Restart scene"))
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    // ---------- Drawing helpers ----------

    void Header(string text)
    {
        GUILayout.Space(8f);
        GUILayout.Label(text, headerStyle);
    }

    // Only writes the value when the slider is actually dragged, so a value set outside the
    // slider's range in the Inspector is left alone
    void Slider(string label, ref float value, float min, float max)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, GUILayout.Width(LabelWidth));
        float shown = Mathf.Clamp(value, min, max);
        float moved = GUILayout.HorizontalSlider(shown, min, max);
        if (!Mathf.Approximately(moved, shown)) value = Mathf.Round(moved * 10f) / 10f;
        GUILayout.Label(value.ToString("0.0"), GUILayout.Width(36f));
        GUILayout.EndHorizontal();
    }

    void IntSlider(string label, ref int value, int min, int max)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, GUILayout.Width(LabelWidth));
        int shown = Mathf.Clamp(value, min, max);
        int moved = Mathf.RoundToInt(GUILayout.HorizontalSlider(shown, min, max));
        if (moved != shown) value = moved;
        GUILayout.Label(value.ToString(), GUILayout.Width(36f));
        GUILayout.EndHorizontal();
    }
}
