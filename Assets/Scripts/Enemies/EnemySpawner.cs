using UnityEngine;
using System.Collections.Generic;

public class EnemySpawner : MonoBehaviour
{
    public static EnemySpawner Instance { get; private set; }

    [Header("Enemy Prefabs")]
    public GameObject basicEnemyPrefab;
    public GameObject fastEnemyPrefab;
    public GameObject tankEnemyPrefab;

    [Header("Spawn Settings")]
    public float arenaWidth = 32f;
    public float arenaHeight = 24f;
    public int maxEnemies = 30;
    public int baseSpawnCount = 2;
    public float spawnMargin = 0.5f;

    [Header("Boss Settings")]
    public GameObject bossEnemyPrefab;
    public int bossWaveInterval = 5;

    [Header("Elite Enemies")]
    [Range(0, 1)] public float eliteChanceBase = 0.05f;
    public float eliteChancePerWave = 0.03f;

    [Header("Enemy Type Weights")]
    [Range(0, 100)] public float basicWeight = 60f;
    [Range(0, 100)] public float fastWeight = 25f;
    [Range(0, 100)] public float tankWeight = 15f;

    private List<EnemyBase> activeEnemies = new List<EnemyBase>();
    private List<BossEnemy> activeBosses = new List<BossEnemy>();
    private float spawnTimer;
    private float currentSpawnInterval = 1.5f;
    private int currentWave = 1;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        spawnTimer = 1f; // Initial delay before first spawn
    }

    void Update()
    {
        if (GameManager.Instance.IsGameOver) return;

        // Don't spawn during rest or clear-wait between waves
        if (GameManager.Instance.IsResting || GameManager.Instance.IsWaitingForClear) return;

        // Clean up destroyed enemies from list
        activeEnemies.RemoveAll(e => e == null);
        activeBosses.RemoveAll(b => b == null);

        spawnTimer -= Time.deltaTime;

        if (spawnTimer <= 0f && activeEnemies.Count < maxEnemies)
        {
            SpawnWave();
            float multiplier = GameManager.Instance.GetSpawnRateMultiplier();
            currentSpawnInterval = Mathf.Max(0.6f, 2.5f / multiplier);
            spawnTimer = currentSpawnInterval;
        }
    }

    void SpawnWave()
    {
        int count = baseSpawnCount + Mathf.FloorToInt(currentWave * 0.8f);
        count = Mathf.Min(count, maxEnemies - activeEnemies.Count);

        for (int i = 0; i < count; i++)
        {
            SpawnEnemy();
        }
    }

    void SpawnEnemy()
    {
        // Pick enemy type based on weights
        GameObject prefab = ChooseEnemyType();
        if (prefab == null) return;

        Vector2 spawnPos = GetSpawnPosition();
        GameObject enemyObj = Instantiate(prefab, new Vector3(spawnPos.x, spawnPos.y, -1f), Quaternion.identity);
        EnemyBase enemy = enemyObj.GetComponent<EnemyBase>();
        if (enemy != null)
        {
            enemy.healthMultiplier = GameManager.Instance.GetEnemyHealthMultiplier();
            enemy.speedMultiplier = GameManager.Instance.GetEnemySpeedMultiplier();
        }
        // Prefab templates are active (not inactive) — no need to SetActive(true)

        if (enemy != null)
        {
            activeEnemies.Add(enemy);
        }

        // Roll for elite (with per-wave override)
        float eliteChance = EliteChanceOverride >= 0f ? EliteChanceOverride
            : eliteChanceBase + currentWave * eliteChancePerWave;
        if (Random.value < eliteChance)
        {
            MakeElite(enemy);
        }

        // Reset per-wave overrides after first spawn
        EliteChanceOverride = -1f;
        FastWeightOverride = -1f;
        TankWeightOverride = -1f;
    }

    void MakeElite(EnemyBase enemy)
    {
        if (enemy == null) return;

        // Increase stats
        enemy.healthMultiplier *= 2f;
        enemy.maxHealth = Mathf.RoundToInt(enemy.maxHealth * 2f);
        enemy.contactDamage = Mathf.RoundToInt(enemy.contactDamage * 1.5f);
        enemy.moveSpeed *= 1.2f;
        enemy.xpDropAmount *= 3f;
        enemy.scoreValue *= 2;

        // Visual — gold color with larger scale
        enemy.transform.localScale = Vector3.one * 1.3f;
        if (enemy.spriteRenderer != null)
        {
            enemy.enemyColor = new Color(1f, 0.7f, 0.1f); // Gold
            enemy.spriteRenderer.color = enemy.enemyColor;
        }

        // Spawn effect
        VFXManager.Instance?.SpawnImpact(enemy.transform.position, new Color(1f, 0.7f, 0.1f), 0.6f);
        CameraFollow.Instance?.Shake(0.15f);
    }

    GameObject ChooseEnemyType()
    {
        float diff = GameManager.Instance.GetDifficultyFactor();

        // Apply per-wave overrides
        float fw = FastWeightOverride >= 0f ? fastWeight * (1f + FastWeightOverride * 5f) : fastWeight;
        float tw = TankWeightOverride >= 0f ? tankWeight * (1f + TankWeightOverride * 5f) : tankWeight;

        // As difficulty increases, more fast & tank enemies
        float adjustedBasic = basicWeight * (1f - diff * 0.3f);
        float adjustedFast = fw * (1f + diff * 0.2f);
        float adjustedTank = tw * (1f + diff * 0.4f);
        float adjTotal = adjustedBasic + adjustedFast + adjustedTank;

        float roll = Random.Range(0f, adjTotal);

        if (roll < adjustedBasic) return basicEnemyPrefab;
        if (roll < adjustedBasic + adjustedFast) return fastEnemyPrefab;
        return tankEnemyPrefab;
    }

    Vector2 GetSpawnPosition()
    {
        Vector2 playerPos = PlayerController.Instance != null
            ? (Vector2)PlayerController.Instance.transform.position
            : Vector2.zero;

        // Spawn at a random angle from the player, inside the arena walls
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 9f;
        Vector2 spawnPos = playerPos + offset;

        // Clamp to stay inside arena walls (with margin)
        float halfW = arenaWidth / 2f - spawnMargin;
        float halfH = arenaHeight / 2f - spawnMargin;
        spawnPos.x = Mathf.Clamp(spawnPos.x, -halfW, halfW);
        spawnPos.y = Mathf.Clamp(spawnPos.y, -halfH, halfH);

        return spawnPos;
    }

    public void OnWaveChanged(int wave, GameManager.WaveType waveType = GameManager.WaveType.Standard)
    {
        currentWave = wave;

        // Adjust composition based on wave type
        switch (waveType)
        {
            case GameManager.WaveType.Horde:
                EliteChanceOverride = -1f; // no elites, just lots of enemies
                break;
            case GameManager.WaveType.Swarm:
                FastWeightOverride = 0.7f;
                break;
            case GameManager.WaveType.Bulwark:
                TankWeightOverride = 0.7f;
                break;
            case GameManager.WaveType.Elite:
                EliteChanceOverride = 1f; // force elites
                break;
            default:
                EliteChanceOverride = -1f;
                FastWeightOverride = -1f;
                TankWeightOverride = -1f;
                break;
        }

        // Boss wave — spawn a boss immediately
        if (bossEnemyPrefab != null && wave % bossWaveInterval == 0)
        {
            SpawnBoss();
        }
    }

    // Per-wave overrides — set by OnWaveChanged, reset on next spawn
    private float EliteChanceOverride = -1f;
    private float FastWeightOverride = -1f;
    private float TankWeightOverride = -1f;

    void SpawnBoss()
    {
        // Pick a spawn position reasonably far from the player
        Vector2 playerPos = PlayerController.Instance != null
            ? (Vector2)PlayerController.Instance.transform.position
            : Vector2.zero;

        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 5f;
        Vector2 spawnPos = playerPos + offset;

        float halfW = arenaWidth / 2f - spawnMargin;
        float halfH = arenaHeight / 2f - spawnMargin;
        spawnPos.x = Mathf.Clamp(spawnPos.x, -halfW, halfW);
        spawnPos.y = Mathf.Clamp(spawnPos.y, -halfH, halfH);

        GameObject bossObj = Instantiate(bossEnemyPrefab, new Vector3(spawnPos.x, spawnPos.y, -1f), Quaternion.identity);
        BossEnemy boss = bossObj.GetComponent<BossEnemy>();
        if (boss != null)
        {
            boss.healthMultiplier = 1f; // bosses have fixed high HP
            boss.speedMultiplier = 1f;
            activeBosses.Add(boss);
        }
        // Prefab template is active — no need to SetActive(true)

        // Big fanfare effect
        VFXManager.Instance?.SpawnExplosion(spawnPos, new Color(0.6f, 0.2f, 0.9f), 2f);
        CameraFollow.Instance?.Shake(0.5f);
    }

    public bool HasAliveEnemies()
    {
        activeEnemies.RemoveAll(e => e == null);
        activeBosses.RemoveAll(b => b == null);
        return activeEnemies.Count > 0 || activeBosses.Count > 0;
    }

    public bool HasAliveBoss()
    {
        activeBosses.RemoveAll(b => b == null);
        return activeBosses.Count > 0;
    }

    public void OnBossKilled()
    {
        activeBosses.RemoveAll(b => b == null);
        if (activeBosses.Count == 0)
        {
            UIManager.Instance?.HideBossHealthBar();
        }
    }

    public void OnEnemyKilled()
    {
        PlayerController.Instance?.OnEnemyKilled();
    }

    public void RegisterEnemy(EnemyBase enemy)
    {
        if (!activeEnemies.Contains(enemy))
            activeEnemies.Add(enemy);
    }

    public void ClearAllEnemies()
    {
        foreach (var enemy in activeEnemies)
        {
            if (enemy != null) Destroy(enemy.gameObject);
        }
        activeEnemies.Clear();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, new Vector3(arenaWidth, arenaHeight, 0));
    }
}
