using UnityEngine;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum WaveType { Standard, Horde, Swarm, Bulwark, Elite }

    [Header("Game State")]
    public int Score { get; private set; }
    public int Wave { get; private set; } = 1;
    public WaveType CurrentWaveType { get; private set; } = WaveType.Standard;
    public float GameTime { get; private set; }
    public bool IsGameOver { get; private set; }
    public bool IsPaused { get; private set; }

    [Header("Difficulty")]
    public float difficultyRampTime = 120f;
    public AnimationCurve spawnRateCurve = AnimationCurve.EaseInOut(0, 1, 120, 3);
    public AnimationCurve enemyHealthCurve = AnimationCurve.EaseInOut(0, 1, 120, 3.5f);
    public AnimationCurve enemySpeedCurve = AnimationCurve.EaseInOut(0, 1, 120, 1.8f);

    [Header("Wave Settings")]
    public float waveDuration = 30f;
    public int enemiesPerWaveBase = 5;
    public float restDuration = 4f; // pause between waves to collect XP

    public bool IsResting { get; private set; }
    public bool IsWaitingForClear { get; private set; }
    public bool GameStarted { get; private set; }

    private float waveTimer;
    private float restTimer;
    private float clearWaitTimer;
    private const float MAX_CLEAR_WAIT = 30f; // force next wave even if enemies remain

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        Time.timeScale = 0f; // paused until player dismisses title screen
        // Wave banner and game timer start when player presses a key
    }

    /// <summary>
    /// Called when the player presses any key on the title screen.
    /// </summary>
    public void StartGame()
    {
        if (GameStarted) return;
        GameStarted = true;
        Time.timeScale = 1f;
        waveTimer = waveDuration;
        UIManager.Instance?.HideTitleScreen();
        UIManager.Instance?.ShowWaveBanner(Wave);
    }

    void Update()
    {
        if (!GameStarted || IsGameOver) return;

        GameTime += Time.deltaTime;

        // Wave rest period — no enemies spawn, player collects XP
        if (IsResting)
        {
            restTimer -= Time.deltaTime;
            if (restTimer <= 0f)
            {
                EndRest();
            }
            return;
        }

        // Waiting for wave to be cleared before rest/next-wave transition
        if (IsWaitingForClear)
        {
            clearWaitTimer -= Time.deltaTime;
            bool cleared = EnemySpawner.Instance == null || !EnemySpawner.Instance.HasAliveEnemies();
            if (cleared || clearWaitTimer <= 0f)
            {
                IsWaitingForClear = false;
                StartRest();
            }
            return;
        }

        waveTimer -= Time.deltaTime;

        if (waveTimer <= 0f)
        {
            // Don't transition yet if enemies are still alive
            if (EnemySpawner.Instance != null && EnemySpawner.Instance.HasAliveEnemies())
            {
                IsWaitingForClear = true;
                clearWaitTimer = MAX_CLEAR_WAIT;
            }
            else
            {
                StartRest();
            }
        }

        // Pause toggle
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            TogglePause();
        }
    }

    void StartRest()
    {
        IsResting = true;
        restTimer = restDuration;
        UIManager.Instance?.ShowWaveCleared();
    }

    void EndRest()
    {
        IsResting = false;
        NextWave();
    }

    void NextWave()
    {
        Wave++;
        waveTimer = waveDuration;

        // Choose a random wave type (avoid repeating the previous one)
        WaveType prevType = CurrentWaveType;
        var allTypes = new WaveType[] { WaveType.Standard, WaveType.Horde, WaveType.Swarm, WaveType.Bulwark, WaveType.Elite };
        do { CurrentWaveType = allTypes[Random.Range(0, allTypes.Length)]; }
        while (CurrentWaveType == prevType && Wave > 2);

        bool isBossWave = Wave % 5 == 0;
        if (isBossWave)
            UIManager.Instance?.ShowWaveBannerBoss(Wave);
        else
            UIManager.Instance?.ShowWaveBanner(Wave, CurrentWaveType);

        EnemySpawner.Instance?.OnWaveChanged(Wave, CurrentWaveType);
    }

    public static string GetWaveTypeLabel(WaveType type)
    {
        switch (type)
        {
            case WaveType.Horde:   return "HORDE";
            case WaveType.Swarm:   return "SWARM";
            case WaveType.Bulwark: return "BULWARK";
            case WaveType.Elite:   return "ELITE";
            default:               return "";
        }
    }

    public void AddScore(int points)
    {
        Score += points;
        UIManager.Instance?.UpdateScoreUI(Score);
    }

    public float GetDifficultyFactor()
    {
        return Mathf.Clamp01(GameTime / difficultyRampTime);
    }

    public float GetSpawnRateMultiplier() => spawnRateCurve.Evaluate(GameTime);
    public float GetEnemyHealthMultiplier() => enemyHealthCurve.Evaluate(GameTime);
    public float GetEnemySpeedMultiplier() => enemySpeedCurve.Evaluate(GameTime);

    public void GameOver()
    {
        if (IsGameOver) return;
        IsGameOver = true;
        Time.timeScale = 0f;
        UIManager.Instance?.ShowGameOver(Score, Wave, GameTime);
    }

    public void RestartGame()
    {
        Bootstrap.RestartGame();
    }

    public void TogglePause()
    {
        IsPaused = !IsPaused;
        Time.timeScale = IsPaused ? 0f : 1f;
        UIManager.Instance?.TogglePauseMenu(IsPaused);
    }
}
