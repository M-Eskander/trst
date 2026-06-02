using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("HUD")]
    public RectTransform healthBarFillRt;
    public Image healthBarBg;
    public TMP_Text healthText;
    public RectTransform xpBarFillRt;
    public TMP_Text xpText;
    public TMP_Text scoreText;
    public TMP_Text waveText;
    public TMP_Text levelText;
    public TMP_Text timerText;

    [Header("Wave Banner")]
    public GameObject waveBanner;
    public TMP_Text waveBannerText;
    public float waveBannerDuration = 2f;

    [Header("Level Up")]
    public GameObject levelUpPanel;
    public Transform upgradeCardContainer;
    public GameObject upgradeCardPrefab;
    public TMP_Text levelUpTitleText;

    [Header("Game Over")]
    public GameObject gameOverPanel;
    public TMP_Text finalScoreText;
    public TMP_Text finalWaveText;
    public TMP_Text finalTimeText;
    public TMP_Text highScoreText;
    public TMP_Text gameOverTitleText;

    [Header("Pause")]
    public GameObject pausePanel;

    [Header("Wave Clear Banner")]
    public GameObject waveClearBanner;
    public TMP_Text waveClearText;
    public float waveClearDuration = 3f;

    [Header("Boss Health Bar")]
    public GameObject bossHealthBarObj;
    public RectTransform bossHealthBarFillRt;
    public Image bossHealthBarFillImage;
    public TMP_Text bossNameText;

    [Header("Title Screen")]
    public GameObject titleScreen;
    public TMP_Text titlePressKeyText;

    [Header("Damage Vignette")]
    public Image damageVignette;

    [Header("Screen Flash")]
    public Image screenFlash;

    [Header("Combat Text")]
    public Transform canvasTransform;

    private int highScore;
    private Coroutine damageFlashRoutine;
    private Coroutine titleBlinkRoutine;
    private Image healthBarFillImage;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        highScore = PlayerPrefs.GetInt("HighScore", 0);

        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (levelUpPanel != null) levelUpPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);
        if (waveBanner != null) waveBanner.SetActive(false);
        if (waveClearBanner != null) waveClearBanner.SetActive(false);
        if (bossHealthBarObj != null) bossHealthBarObj.SetActive(false);

        UpdateScoreUI(0);
        UpdateHealthBar(1f);
        UpdateXPBar(0f);
        UpdateWaveUI(1);
        UpdateLevelUI(1);

        // Cache health bar fill image for color changes
        if (healthBarFillRt != null)
            healthBarFillImage = healthBarFillRt.GetComponent<Image>();
    }

    void Update()
    {
        if (timerText != null && !GameManager.Instance.IsGameOver)
        {
            float t = GameManager.Instance.GameTime;
            int minutes = Mathf.FloorToInt(t / 60f);
            int seconds = Mathf.FloorToInt(t % 60f);
            timerText.text = $"{minutes:00}:{seconds:00}";
        }

        // Pulse wave text when waiting for enemies to be cleared
        if (GameManager.Instance.IsWaitingForClear && waveText != null)
        {
            float pulse = Mathf.Sin(Time.unscaledTime * 6f) * 0.3f + 0.7f;
            waveText.color = new Color(1f, 0.5f, 0.2f, pulse);
            waveText.text = $"WAVE {GameManager.Instance.Wave} - CLEAR!";
        }
        else if (waveText != null && !GameManager.Instance.IsWaitingForClear && waveText.text.Contains("CLEAR"))
        {
            waveText.color = new Color(0.3f, 0.8f, 1f);
            waveText.text = $"WAVE {GameManager.Instance.Wave}";
        }

        if (GameManager.Instance.IsGameOver)
        {
            if (waveText != null) waveText.gameObject.SetActive(false);
            if (timerText != null) timerText.gameObject.SetActive(false);
            if (levelText != null) levelText.gameObject.SetActive(false);
        }
    }

    public void UpdateScoreUI(int score)
    {
        if (scoreText != null) scoreText.text = score.ToString("N0");
    }

    public void UpdateHealthBar(float fill)
    {
        if (healthBarFillRt != null)
        {
            float pct = Mathf.Clamp01(fill);
            healthBarFillRt.anchorMin = Vector2.zero;
            healthBarFillRt.anchorMax = new Vector2(pct, 1f);
            healthBarFillRt.offsetMin = new Vector2(2, 2);
            healthBarFillRt.offsetMax = new Vector2(-2, -2);

            // Color changes: green > yellow > red as HP drops
            if (healthBarFillImage != null)
            {
                if (pct > 0.5f)
                    healthBarFillImage.color = Color.Lerp(new Color(0.6f, 0.9f, 0.2f), new Color(0.2f, 0.9f, 0.2f), (pct - 0.5f) * 2f);
                else if (pct > 0.25f)
                    healthBarFillImage.color = Color.Lerp(new Color(0.9f, 0.6f, 0f), new Color(0.6f, 0.9f, 0.2f), pct * 2f);
                else
                    healthBarFillImage.color = Color.Lerp(new Color(0.9f, 0.1f, 0.1f), new Color(0.9f, 0.6f, 0f), pct * 4f);
            }
        }
        if (healthText != null && PlayerController.Instance != null)
        {
            int hp = Mathf.Max(0, PlayerController.Instance.currentHealth);
            healthText.text = $"{hp} / {PlayerController.Instance.maxHealth}";
        }
    }

    public void UpdateXPBar(float fill)
    {
        if (xpBarFillRt != null)
        {
            float pct = Mathf.Clamp01(fill);
            xpBarFillRt.anchorMin = Vector2.zero;
            xpBarFillRt.anchorMax = new Vector2(pct, 1f);
            xpBarFillRt.offsetMin = new Vector2(2, 2);
            xpBarFillRt.offsetMax = new Vector2(-2, -2);
        }
        if (xpText != null && UpgradeManager.Instance != null)
        {
            xpText.text = $"XP {UpgradeManager.Instance.currentXP:F0} / {UpgradeManager.Instance.xpToNextLevel:F0}";
        }
    }

    public void UpdateWaveUI(int wave)
    {
        if (waveText != null) waveText.text = $"WAVE {wave}";
    }

    public void UpdateLevelUI(int level)
    {
        if (levelText != null) levelText.text = $"LV {level}";
    }

    public void ShowWaveBanner(int wave, GameManager.WaveType waveType = GameManager.WaveType.Standard)
    {
        if (waveBanner == null || waveBannerText == null) return;
        waveBanner.SetActive(true);
        var img = waveBanner.GetComponent<Image>();
        if (img != null) img.color = new Color(0.15f, 0.05f, 0.35f, 0.9f);

        waveBanner.transform.localScale = Vector3.one * 1.3f;
        StartCoroutine(ScaleIn(waveBanner.transform, 0.15f));

        string label = GameManager.GetWaveTypeLabel(waveType);
        if (!string.IsNullOrEmpty(label))
            waveBannerText.text = $"WAVE {wave} — {label}";
        else
            waveBannerText.text = $"WAVE {wave}";
        waveBannerText.color = new Color(0.5f, 0.7f, 1f);

        CancelInvoke(nameof(HideWaveBanner));
        Invoke(nameof(HideWaveBanner), waveBannerDuration);
    }

    public void ShowWaveBannerBoss(int wave)
    {
        if (waveBanner == null || waveBannerText == null) return;
        waveBanner.SetActive(true);
        var img = waveBanner.GetComponent<Image>();
        if (img != null) img.color = new Color(0.5f, 0.05f, 0.15f, 0.95f);

        waveBanner.transform.localScale = Vector3.one * 1.5f;
        StartCoroutine(ScaleIn(waveBanner.transform, 0.2f));

        waveBannerText.text = $"⚠ WAVE {wave} - BOSS! ⚠";
        waveBannerText.color = new Color(1f, 0.6f, 0f);
        CancelInvoke(nameof(HideWaveBanner));
        Invoke(nameof(HideWaveBanner), waveBannerDuration + 1.5f);
    }

    IEnumerator ScaleIn(Transform t, float duration)
    {
        float elapsed = 0;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float s = Mathf.SmoothStep(1.3f, 1f, elapsed / duration);
            t.localScale = Vector3.one * s;
            yield return null;
        }
        t.localScale = Vector3.one;
    }

    void HideWaveBanner()
    {
        if (waveBanner != null) waveBanner.SetActive(false);
    }

    public void ShowWaveCleared()
    {
        if (waveClearBanner == null || waveClearText == null) return;
        waveClearBanner.SetActive(true);
        waveClearBanner.transform.localScale = Vector3.one * 1.2f;
        StartCoroutine(ScaleIn(waveClearBanner.transform, 0.2f));
        waveClearText.text = "WAVE CLEARED!";
        CancelInvoke(nameof(HideWaveClearBanner));
        Invoke(nameof(HideWaveClearBanner), waveClearDuration);
    }

    void HideWaveClearBanner()
    {
        if (waveClearBanner != null) waveClearBanner.SetActive(false);
    }

    public void ShowBossHealthBar(string variant = "")
    {
        if (bossHealthBarObj == null) return;
        bossHealthBarObj.SetActive(true);
        if (bossHealthBarFillRt != null)
        {
            bossHealthBarFillRt.anchorMin = Vector2.zero;
            bossHealthBarFillRt.anchorMax = Vector2.one;
            bossHealthBarFillRt.offsetMin = new Vector2(2, 2);
            bossHealthBarFillRt.offsetMax = new Vector2(-2, -2);
        }
        if (bossHealthBarFillImage != null)
            bossHealthBarFillImage.color = new Color(0.8f, 0.2f, 0.5f);
        if (bossNameText != null)
        {
            string label = string.IsNullOrEmpty(variant) ? "BOSS" : variant;
            bossNameText.text = $"<color=#FF66CC>✦</color> {label} <color=#FF66CC>✦</color>";
        }
    }

    public void UpdateBossHealthBar(float fill)
    {
        if (bossHealthBarFillRt != null)
        {
            float pct = Mathf.Clamp01(fill);
            bossHealthBarFillRt.anchorMin = Vector2.zero;
            bossHealthBarFillRt.anchorMax = new Vector2(pct, 1f);
            bossHealthBarFillRt.offsetMin = new Vector2(2, 2);
            bossHealthBarFillRt.offsetMax = new Vector2(-2, -2);

            // Color shifts as boss takes damage
            if (bossHealthBarFillImage != null)
            {
                if (pct > 0.5f)
                    bossHealthBarFillImage.color = Color.Lerp(new Color(0.9f, 0.3f, 0.6f), new Color(0.6f, 0.2f, 0.9f), (pct - 0.5f) * 2f);
                else
                    bossHealthBarFillImage.color = Color.Lerp(new Color(0.9f, 0.2f, 0.2f), new Color(0.9f, 0.3f, 0.6f), pct * 2f);
            }
        }
    }

    public void HideBossHealthBar()
    {
        if (bossHealthBarObj != null) bossHealthBarObj.SetActive(false);
    }

    public void ShowLevelUpUI(int level)
    {
        if (levelUpPanel == null) return;
        levelUpPanel.SetActive(true);
        if (levelUpTitleText != null) levelUpTitleText.text = $"LEVEL {level}!";
        UpdateLevelUI(level);

        // Animate in
        levelUpPanel.GetComponent<Image>().color = new Color(0, 0, 0, 0);

        // Clear old cards
        foreach (Transform child in upgradeCardContainer)
        {
            Destroy(child.gameObject);
        }

        var choices = UpgradeManager.Instance.GetUpgradeChoices(3);
        foreach (var upgrade in choices)
        {
            GameObject card = Instantiate(upgradeCardPrefab, upgradeCardContainer);
            card.SetActive(true);
            var uiCard = card.GetComponent<UpgradeCardUI>();
            if (uiCard != null)
            {
                uiCard.Setup(upgrade);
            }
        }
    }

    public void HideLevelUpUI()
    {
        if (levelUpPanel != null) levelUpPanel.SetActive(false);
    }

    public void ShowGameOver(int score, int wave, float time)
    {
        if (gameOverPanel == null) return;
        gameOverPanel.SetActive(true);

        if (gameOverTitleText != null)
            gameOverTitleText.text = "GAME OVER";

        if (finalScoreText != null) finalScoreText.text = $"Score: {score:N0}";
        if (finalWaveText != null) finalWaveText.text = $"Wave Reached: {wave}";
        if (finalTimeText != null)
        {
            int m = Mathf.FloorToInt(time / 60f);
            int s = Mathf.FloorToInt(time % 60f);
            finalTimeText.text = $"Time: {m:00}:{s:00}";
        }

        if (score > highScore)
        {
            highScore = score;
            PlayerPrefs.SetInt("HighScore", highScore);
            PlayerPrefs.Save();
            if (highScoreText != null) highScoreText.text = "🏆 NEW HIGH SCORE! 🏆";
        }
        else
        {
            if (highScoreText != null) highScoreText.text = $"High Score: {highScore:N0}";
        }
    }

    public void TogglePauseMenu(bool paused)
    {
        if (pausePanel != null) pausePanel.SetActive(paused);
    }

    public void SpawnDamageText(Vector2 worldPos, int damage, Color color)
    {
        if (canvasTransform == null) return;
        Camera cam = Camera.main;
        if (cam == null) return;

        GameObject textObj = new GameObject("DamageText");
        textObj.transform.SetParent(canvasTransform, false);
        textObj.SetActive(true);

        var rt = textObj.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(140, 50);

        var tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = damage.ToString();
        tmp.fontSize = damage > 50 ? 48 : 36;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Midline;
        tmp.fontStyle = FontStyles.Bold;
        tmp.outlineWidth = 0.3f;
        tmp.outlineColor = Color.black;

        textObj.transform.position = cam.WorldToScreenPoint(worldPos);

        // Floating + fade out
        StartCoroutine(FloatDamageText(textObj));
    }

    IEnumerator FloatDamageText(GameObject obj)
    {
        float t = 0;
        Vector3 startPos = obj.transform.position;
        float rise = 30f;
        while (t < 0.6f)
        {
            t += Time.deltaTime;
            obj.transform.position = startPos + Vector3.up * rise * (t / 0.6f);
            var tmp = obj.GetComponent<TextMeshProUGUI>();
            if (tmp != null) tmp.alpha = 1f - (t / 0.6f);
            yield return null;
        }
        Destroy(obj);
    }

    public void DamageFlash()
    {
        if (damageFlashRoutine != null) StopCoroutine(damageFlashRoutine);
        damageFlashRoutine = StartCoroutine(DamageFlashRoutine());
    }

    IEnumerator DamageFlashRoutine()
    {
        float duration = 0.15f;
        float elapsed = 0;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float a = Mathf.Lerp(0.4f, 0f, elapsed / duration);
            if (damageVignette != null) damageVignette.color = new Color(1, 0, 0, a);
            yield return null;
        }
        if (damageVignette != null) damageVignette.color = Color.clear;
    }

    public void FlashScreen(Color color, float duration = 0.3f)
    {
        StartCoroutine(FlashRoutine(color, duration));
    }

    IEnumerator FlashRoutine(Color color, float duration)
    {
        float elapsed = 0;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float a = Mathf.Lerp(color.a, 0f, elapsed / duration);
            if (screenFlash != null) screenFlash.color = new Color(color.r, color.g, color.b, a);
            yield return null;
        }
        if (screenFlash != null) screenFlash.color = Color.clear;
    }

    public void ShowTitleScreen()
    {
        if (titleScreen != null) titleScreen.SetActive(true);
        if (titlePressKeyText != null)
        {
            if (titleBlinkRoutine != null) StopCoroutine(titleBlinkRoutine);
            titleBlinkRoutine = StartCoroutine(BlinkTitleText());
        }
    }

    public void HideTitleScreen()
    {
        if (titleScreen != null) titleScreen.SetActive(false);
        if (titleBlinkRoutine != null)
        {
            StopCoroutine(titleBlinkRoutine);
            titleBlinkRoutine = null;
        }
    }

    IEnumerator BlinkTitleText()
    {
        while (true)
        {
            if (titlePressKeyText != null)
            {
                float alpha = Mathf.PingPong(Time.unscaledTime * 1.5f, 1f);
                titlePressKeyText.alpha = alpha;
            }
            yield return null;
        }
    }

    public void RestartGame()
    {
        // Reactivate HUD elements before restart
        if (waveText != null) waveText.gameObject.SetActive(true);
        if (timerText != null) timerText.gameObject.SetActive(true);
        if (levelText != null) levelText.gameObject.SetActive(true);
        Bootstrap.RestartGame();
    }

    public void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
