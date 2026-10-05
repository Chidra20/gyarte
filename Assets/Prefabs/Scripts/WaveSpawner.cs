using System;
using System.Collections.Generic;
using UnityEngine;

// Sends waves of enemies at the player in a generated level. Each wave spawns on random floor spots,
// away from the player and not bunched together. A wave is over when no enemy is left alive
// (babies count too); after a short breather the next one comes, and after the last one AllWavesCleared fires.
public class WaveSpawner : MonoBehaviour
{
    [Header("References (found automatically when empty)")]
    public LevelRandomizer randomizer;
    public Transform player;
    [Tooltip("What each wave is made of.")]
    public GameObject enemyPrefab;

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
    public float minPlayerDistance = 6f;
    [Tooltip("No enemy spawns closer than this to another living enemy.")]
    public float minEnemySpacing = 2f;
    [Tooltip("At most maxPerRadius living enemies may be within this distance of a new spawn.")]
    public float crowdRadius = 5f;
    public int maxPerRadius = 3;
    [Tooltip("Random spots tried per enemy before giving up on it. A wave may come up short in a small level.")]
    public int spawnAttemptsPerEnemy = 15;

    public int CurrentWave { get; private set; }
    public int TotalWaves { get; private set; }
    public bool Running { get; private set; }
    // Seconds until the next wave, 0 while a wave is being fought
    public float NextWaveIn => Running && !waveActive ? Mathf.Max(0f, nextWaveTime - Time.time) : 0f;

    public event Action<int> WaveStarted;
    public event Action AllWavesCleared;

    private bool waveActive;
    private float nextWaveTime;
    private int level;
    private Transform enemiesParent;
    private readonly List<BigSlime> toAlert = new List<BigSlime>();

    public static int WaveCount(int level, int baseWaves, float wavesPerLevel, int maxWaves)
    {
        return Mathf.Min(maxWaves, baseWaves + Mathf.FloorToInt(level * wavesPerLevel));
    }

    public static int WaveSize(int level, int baseEnemies, int enemiesPerLevel, int maxEnemies)
    {
        return Mathf.Min(maxEnemies, baseEnemies + level * enemiesPerLevel);
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
        toAlert.Clear();
        foreach (EnemyHealth enemy in new List<EnemyHealth>(EnemyHealth.Alive)) Destroy(enemy.gameObject);
    }

    // Removes the living enemies; the wave then counts as cleared as usual
    public void KillAll()
    {
        foreach (EnemyHealth enemy in new List<EnemyHealth>(EnemyHealth.Alive)) Destroy(enemy.gameObject);
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
        // Slimes ignore Alert until their Start has run, so they are woken a frame after spawning
        foreach (BigSlime slime in toAlert)
        {
            if (slime != null) slime.Alert();
        }
        toAlert.Clear();

        if (!Running) return;

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

    void SpawnWave()
    {
        CurrentWave++;
        waveActive = true;

        int size = WaveSize(level, baseEnemiesPerWave, enemiesPerLevel, maxEnemiesPerWave);
        int spawned = 0;
        if (enemyPrefab != null && randomizer != null && randomizer.Rooms.Count > 0 && player != null)
        {
            if (enemiesParent == null) enemiesParent = new GameObject("Enemies").transform;

            var taken = new List<Vector2>();
            foreach (EnemyHealth enemy in EnemyHealth.Alive) taken.Add(enemy.transform.position);

            for (int i = 0; i < size; i++)
            {
                if (!TryFindSpawn(SampleFloorPoint, p => IsValidSpawn(p, player.position, taken,
                        minPlayerDistance, minEnemySpacing, crowdRadius, maxPerRadius), spawnAttemptsPerEnemy, out Vector2 point))
                {
                    continue;
                }

                GameObject enemy = Instantiate(enemyPrefab, new Vector3(point.x, point.y, player.position.z), Quaternion.identity, enemiesParent);
                taken.Add(point);
                spawned++;

                BigSlime slime = enemy.GetComponent<BigSlime>();
                if (slime != null) toAlert.Add(slime);
            }
        }

        if (spawned < size) Debug.Log("Wave " + CurrentWave + ": spawned " + spawned + " of " + size + " (no room left that keeps the spacing).");
        WaveStarted?.Invoke(CurrentWave);
    }

    Vector2? SampleFloorPoint()
    {
        int room = UnityEngine.Random.Range(0, randomizer.Rooms.Count);
        return randomizer.TryGetRandomFloorPoint(room, out Vector2 point) ? point : (Vector2?)null;
    }
}
