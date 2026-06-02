using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using System.Collections.Generic;

/// <summary>
/// Entry point that creates the entire game setup at runtime.
/// </summary>
public class Bootstrap : MonoBehaviour
{
    /// <summary>Tracks all root GameObjects created so we can destroy them on restart.</summary>
    static List<GameObject> rootObjects = new List<GameObject>();

    void Awake()
    {
        // Clean any leftover objects from a previous run (e.g. if script was recompiled)
        CleanupOldObjects();

        // Set up the scene - order matters for dependencies.
        // Each step is isolated so one failure doesn't silently kill the rest.
        SafeCreate(CreateGameSystems, "GameSystems");
        SafeCreate(CreateCameraFollow, "CameraFollow");
        SafeCreate(CreateArena, "Arena");
        SafeCreate(CreateAudio, "Audio");
        SafeCreate(CreateVFX, "VFX");
        SafeCreate(CreatePlayer, "Player");
        SafeCreate(CreateEnemySpawner, "EnemySpawner");
        SafeCreate(CreateXPOrbPool, "XPOrbPool");
        SafeCreate(CreateUI, "UI");

        // Make sure there's an EventSystem for UI
        if (FindFirstObjectByType<EventSystem>() == null)
        {
            rootObjects.Add(new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule)));
        }

        // Group inactive prefab templates under Bootstrap for a clean hierarchy
        foreach (var obj in rootObjects)
        {
            if (obj != null && !obj.activeSelf && obj.transform.parent == null)
                obj.transform.SetParent(transform);
        }
    }

    void SafeCreate(System.Action createMethod, string label)
    {
        try
        {
            createMethod();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[Bootstrap] Failed to create {label}: {ex.Message}\n{ex.StackTrace}");
        }
    }

    /// <summary>Call this from the restart button instead of scene reload.</summary>
    public static void RestartGame()
    {
        Time.timeScale = 1f;

        // Destroy runtime-spawned enemies/projectiles that aren't in rootObjects
        var enemies = FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
        foreach (var e in enemies)
            if (e != null) DestroyImmediate(e.gameObject);

        // Destroy all runtime projectiles from player and enemies
        var projs = FindObjectsByType<Projectile>(FindObjectsSortMode.None);
        foreach (var p in projs)
            if (p != null) DestroyImmediate(p.gameObject);
        var eprojs = FindObjectsByType<EnemyProjectile>(FindObjectsSortMode.None);
        foreach (var p in eprojs)
            if (p != null) DestroyImmediate(p.gameObject);

        // Destroy every GameObject Bootstrap created (in reverse so nothing
        // tries to re-register itself after its singleton has been destroyed).
        foreach (var obj in rootObjects)
        {
            if (obj != null) DestroyImmediate(obj);
        }
        rootObjects.Clear();

        // Also destroy any Bootstrap component hanging around
        var bootstrap = FindFirstObjectByType<Bootstrap>();
        if (bootstrap != null) DestroyImmediate(bootstrap.gameObject);

        // Create a fresh Bootstrap — its Awake() rebuilds everything
        new GameObject("Bootstrap", typeof(Bootstrap));
    }

    static void CleanupOldObjects()
    {
        foreach (var obj in rootObjects)
        {
            if (obj != null) DestroyImmediate(obj);
        }
        rootObjects.Clear();
    }

    void Track(GameObject obj)
    {
        if (obj != null && !rootObjects.Contains(obj))
            rootObjects.Add(obj);
    }

    void CreateGameSystems()
    {
        GameObject systemsObj = new GameObject("GameSystems");
        systemsObj.AddComponent<GameManager>();
        systemsObj.AddComponent<UpgradeManager>();
        Track(systemsObj);
    }

    void CreatePlayer()
    {
        GameObject playerObj = new GameObject("Player");
        try { playerObj.tag = "Player"; } catch { /* harmless re-registration on restart */ }
        playerObj.layer = LayerMask.NameToLayer("Default");

        var sr = playerObj.AddComponent<SpriteRenderer>();
        Texture2D playerTex = ProceduralAssets.CreatePlayerTexture();
        sr.sprite = Sprite.Create(playerTex, new Rect(0, 0, playerTex.width, playerTex.height), new Vector2(0.5f, 0.5f), 64);
        sr.material = ProceduralAssets.CreateNeonMaterial(Color.cyan, true);
        sr.sortingOrder = 20;

        var rb = playerObj.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.linearDamping = 2f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var collider = playerObj.AddComponent<CircleCollider2D>();
        collider.radius = 0.3f;

        var controller = playerObj.AddComponent<PlayerController>();
        controller.spriteRenderer = sr;

        playerObj.AddComponent<PlayerInputHandler>();

        // Player trail
        var trail = playerObj.AddComponent<TrailRenderer>();
        trail.time = 0.15f;
        trail.startWidth = 0.4f;
        trail.endWidth = 0f;
        trail.startColor = new Color(0, 1, 1, 0.3f);
        trail.endColor = new Color(0, 1, 1, 0f);
        var trailMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        if (trailMat != null) trail.material = trailMat;

        // Create fire point
        GameObject firePoint = new GameObject("FirePoint");
        firePoint.transform.parent = playerObj.transform;
        firePoint.transform.localPosition = new Vector3(0.6f, 0, 0);
        controller.firePoint = firePoint.transform;

        playerObj.transform.position = new Vector3(0, 0, -1);
        Track(playerObj);
    }

    public static GameObject CreateEnemyBasePrefab(string name, ProceduralAssets.EnemyType type, Color color)
    {
        GameObject enemy = new GameObject(name);
        enemy.SetActive(false);

        var sr = enemy.AddComponent<SpriteRenderer>();
        Texture2D tex = ProceduralAssets.CreateEnemyTexture(type);
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 48);
        // Material is neutral white — the actual color is set by EnemyBase.Start() via spriteRenderer.color
        Material mat = ProceduralAssets.CreateNeonMaterial(Color.white, false);
        if (mat == null) mat = new Material(Shader.Find("Sprites/Default"));
        sr.material = mat;
        sr.color = color;
        sr.sortingOrder = 15;

        var rb = enemy.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.linearDamping = 3f;

        var collider = enemy.AddComponent<CircleCollider2D>();
        collider.radius = 0.3f;

        return enemy;
    }

    void CreateEnemySpawner()
    {
        GameObject spawnerObj = new GameObject("EnemySpawner");
        var spawner = spawnerObj.AddComponent<EnemySpawner>();
        Track(spawnerObj);

        // Create enemy prefabs
        GameObject basicEnemy = CreateEnemyBasePrefab("BasicEnemy", ProceduralAssets.EnemyType.Basic, Color.red);
        basicEnemy.AddComponent<EnemyBase>();
        spawner.basicEnemyPrefab = basicEnemy;
        Track(basicEnemy);

        GameObject fastEnemy = CreateEnemyBasePrefab("FastEnemy", ProceduralAssets.EnemyType.Fast, new Color(0.8f, 0.2f, 0.8f));
        fastEnemy.AddComponent<FastEnemy>();
        spawner.fastEnemyPrefab = fastEnemy;
        Track(fastEnemy);

        GameObject tankEnemy = CreateEnemyBasePrefab("TankEnemy", ProceduralAssets.EnemyType.Tank, new Color(0.9f, 0.4f, 0.1f));
        tankEnemy.AddComponent<TankEnemy>();
        spawner.tankEnemyPrefab = tankEnemy;
        Track(tankEnemy);

        // Create boss enemy prefab with unique texture
        GameObject bossEnemy = CreateBossPrefab("BossEnemy");
        bossEnemy.AddComponent<BossEnemy>();
        spawner.bossEnemyPrefab = bossEnemy;
        Track(bossEnemy);
    }

    GameObject CreateBossPrefab(string name)
    {
        GameObject enemy = new GameObject(name);
        enemy.SetActive(false);

        var sr = enemy.AddComponent<SpriteRenderer>();
        Texture2D tex = ProceduralAssets.CreateBossTexture();
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 64);
        Material mat = ProceduralAssets.CreateNeonMaterial(Color.white, true);
        if (mat == null) mat = new Material(Shader.Find("Sprites/Default"));
        sr.material = mat;
        sr.color = new Color(0.6f, 0.2f, 0.9f);
        sr.sortingOrder = 20; // above normal enemies

        var rb = enemy.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.linearDamping = 3f;

        var collider = enemy.AddComponent<CircleCollider2D>();
        collider.radius = 0.4f;

        return enemy;
    }

    void CreateXPOrbPool()
    {
        GameObject poolObj = new GameObject("XPOrbPool");
        var pool = poolObj.AddComponent<XPOrbPool>();
        Track(poolObj);

        // Create XP orb prefab
        GameObject orbPrefab = new GameObject("XPOrb");
        orbPrefab.SetActive(false);

        var sr = orbPrefab.AddComponent<SpriteRenderer>();
        Texture2D orbTex = ProceduralAssets.CreateXPOrbTexture();
        sr.sprite = Sprite.Create(orbTex, new Rect(0, 0, orbTex.width, orbTex.height), new Vector2(0.5f, 0.5f), 24);
        sr.sortingOrder = 12;

        var collider = orbPrefab.AddComponent<CircleCollider2D>();
        collider.radius = 0.15f;
        collider.isTrigger = true;

        orbPrefab.AddComponent<XPOrb>();

        pool.xpOrbPrefab = orbPrefab;
        Track(orbPrefab);
    }

    TMP_FontAsset GetOrCreateFont()
    {
        // Try to find any already-loaded TMP font asset
        var fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
        if (fonts != null && fonts.Length > 0)
            return fonts[0];

        // Try loading from TMP's Resources folder (correct subpath)
        TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (font != null) return font;

        // Try the fallback asset
        font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF - Fallback");
        if (font != null) return font;

        // No TMP font found — TMP will use its built-in fallback, which is fine
        Debug.LogWarning("[Bootstrap] No TMP font asset found — using fallback.");
        return null;
    }

    void CreateUI()
    {
        // Create Canvas
        GameObject canvasObj = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        Track(canvasObj);

        var uiManager = canvasObj.AddComponent<UIManager>();
        canvasObj.AddComponent<VisualEffects>();
        uiManager.canvasTransform = canvasObj.transform;

        TMP_FontAsset font = GetOrCreateFont();

        // ===== DAMAGE VIGNETTE (full-screen red overlay) =====
        GameObject vignetteObj = new GameObject("DamageVignette", typeof(Image));
        vignetteObj.transform.SetParent(canvasObj.transform, false);
        var vignetteRt = vignetteObj.GetComponent<RectTransform>();
        vignetteRt.anchorMin = Vector2.zero;
        vignetteRt.anchorMax = Vector2.one;
        vignetteRt.sizeDelta = Vector2.zero;
        var vignetteImg = vignetteObj.GetComponent<Image>();
        vignetteImg.color = Color.clear;
        vignetteImg.raycastTarget = false;
        uiManager.damageVignette = vignetteImg;

        // ===== SCREEN FLASH (full-screen white/gold overlay) =====
        GameObject flashObj = new GameObject("ScreenFlash", typeof(Image));
        flashObj.transform.SetParent(canvasObj.transform, false);
        var flashRt = flashObj.GetComponent<RectTransform>();
        flashRt.anchorMin = Vector2.zero;
        flashRt.anchorMax = Vector2.one;
        flashRt.sizeDelta = Vector2.zero;
        var flashImg = flashObj.GetComponent<Image>();
        flashImg.color = Color.clear;
        flashImg.raycastTarget = false;
        uiManager.screenFlash = flashImg;

        // ===== HUD =====

        // --- Health Bar (top-left) ---
        // Background panel for health area
        GameObject hpPanel = new GameObject("HPPanel", typeof(Image));
        hpPanel.transform.SetParent(canvasObj.transform, false);
        var hpPanelRt = hpPanel.GetComponent<RectTransform>();
        hpPanelRt.anchorMin = new Vector2(0, 1);
        hpPanelRt.anchorMax = new Vector2(0, 1);
        hpPanelRt.pivot = new Vector2(0, 1);
        hpPanelRt.anchoredPosition = new Vector2(16, -16);
        hpPanelRt.sizeDelta = new Vector2(420, 52);
        var hpPanelImg = hpPanel.GetComponent<Image>();
        hpPanelImg.color = new Color(0, 0, 0, 0.4f);
        hpPanelImg.raycastTarget = false;

        // Health bar inside panel
        GameObject healthBarObj = CreateSlider(hpPanel.transform, "HealthBar", new Vector2(10, -6), new Vector2(320, 28),
            new Color(0.2f, 0.9f, 0.2f), new Color(0.15f, 0.15f, 0.15f, 0.6f), out uiManager.healthBarFillRt);
        uiManager.healthBarBg = healthBarObj.GetComponent<Image>();

        // Heart icon
        GameObject heartObj = CreateTextAtTransform(hpPanel.transform, "♥", 24, TextAlignmentOptions.Midline,
            new Color(1f, 0.3f, 0.3f), font);
        heartObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(-110, 0);
        heartObj.GetComponent<RectTransform>().sizeDelta = new Vector2(40, 40);

        // Health text
        GameObject healthTextObj = CreateTextAtTransform(hpPanel.transform, "100 / 100", 20, TextAlignmentOptions.Midline,
            Color.white, font);
        healthTextObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(200, 0);
        healthTextObj.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 30);
        uiManager.healthText = healthTextObj.GetComponent<TMP_Text>();

        // --- XP Bar (below health) ---
        GameObject xpPanel = new GameObject("XPPanel", typeof(Image));
        xpPanel.transform.SetParent(canvasObj.transform, false);
        var xpPanelRt = xpPanel.GetComponent<RectTransform>();
        xpPanelRt.anchorMin = new Vector2(0, 1);
        xpPanelRt.anchorMax = new Vector2(0, 1);
        xpPanelRt.pivot = new Vector2(0, 1);
        xpPanelRt.anchoredPosition = new Vector2(16, -72);
        xpPanelRt.sizeDelta = new Vector2(420, 40);
        var xpPanelImg = xpPanel.GetComponent<Image>();
        xpPanelImg.color = new Color(0, 0, 0, 0.4f);
        xpPanelImg.raycastTarget = false;

        GameObject xpBarObj = CreateSlider(xpPanel.transform, "XPBar", new Vector2(10, -6), new Vector2(320, 20),
            new Color(0.6f, 0.3f, 0.9f), new Color(0.15f, 0.1f, 0.2f, 0.6f), out uiManager.xpBarFillRt);

        // Star icon
        GameObject starObj = CreateTextAtTransform(xpPanel.transform, "✦", 18, TextAlignmentOptions.Midline,
            new Color(0.8f, 0.6f, 1f), font);
        starObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(-110, 0);
        starObj.GetComponent<RectTransform>().sizeDelta = new Vector2(40, 40);

        // XP text
        GameObject xpTextObj = CreateTextAtTransform(xpPanel.transform, "XP 0 / 20", 16, TextAlignmentOptions.Midline,
            new Color(0.8f, 0.6f, 1f), font);
        xpTextObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(200, 0);
        xpTextObj.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 30);
        uiManager.xpText = xpTextObj.GetComponent<TMP_Text>();

        // --- Score (top-right) ---
        GameObject scoreObj = CreateTextAtParent(canvasObj.transform, "ScoreText", new Vector2(-20, -20),
            "0", 36, TextAlignmentOptions.TopRight, Color.white, font, false, true);
        scoreObj.GetComponent<RectTransform>().sizeDelta = new Vector2(300, 50);
        var scoreTmp = scoreObj.GetComponent<TextMeshProUGUI>();
        scoreTmp.fontStyle = FontStyles.Bold;
        uiManager.scoreText = scoreTmp;

        // Score label
        GameObject scoreLabelObj = CreateTextAtParent(canvasObj.transform, "ScoreLabel", new Vector2(-20, -60),
            "SCORE", 14, TextAlignmentOptions.TopRight, new Color(0.6f, 0.6f, 0.6f), font, false, true);
        scoreLabelObj.GetComponent<RectTransform>().sizeDelta = new Vector2(300, 24);

        // --- Timer (top-right below score) ---
        GameObject timerObj = CreateTextAtParent(canvasObj.transform, "TimerText", new Vector2(-20, -95),
            "00:00", 28, TextAlignmentOptions.TopRight, new Color(0.8f, 0.8f, 0.8f), font, false, true);
        timerObj.GetComponent<RectTransform>().sizeDelta = new Vector2(300, 36);
        uiManager.timerText = timerObj.GetComponent<TMP_Text>();

        // --- Wave (top-center) ---
        // Wave label
        GameObject waveObj = CreateTextAtParent(canvasObj.transform, "WaveText", new Vector2(0, -16),
            "WAVE 1", 28, TextAlignmentOptions.Top, new Color(0.3f, 0.8f, 1f), font, true);
        waveObj.GetComponent<RectTransform>().sizeDelta = new Vector2(300, 36);
        var waveTmp = waveObj.GetComponent<TextMeshProUGUI>();
        waveTmp.fontStyle = FontStyles.Bold;
        uiManager.waveText = waveTmp;

        // --- Level (bottom-left) ---
        GameObject levelObj = CreateTextAtParent(canvasObj.transform, "LevelText", new Vector2(20, -20),
            "LV 1", 18, TextAlignmentOptions.BottomLeft, new Color(1f, 0.8f, 0.2f), font);
        levelObj.GetComponent<RectTransform>().sizeDelta = new Vector2(120, 30);
        uiManager.levelText = levelObj.GetComponent<TMP_Text>();

        // ===== Wave Banner =====
        GameObject bannerObj = new GameObject("WaveBanner", typeof(Image));
        bannerObj.transform.SetParent(canvasObj.transform, false);
        var bannerRt = bannerObj.GetComponent<RectTransform>();
        bannerRt.anchorMin = new Vector2(0.5f, 0.5f);
        bannerRt.anchorMax = new Vector2(0.5f, 0.5f);
        bannerRt.sizeDelta = new Vector2(700, 140);
        bannerRt.anchoredPosition = new Vector2(0, 40);

        var bannerImage = bannerObj.GetComponent<Image>();
        bannerImage.color = new Color(0.15f, 0.05f, 0.35f, 0.9f);

        // Banner border line
        GameObject bannerBorder = new GameObject("BannerBorder", typeof(Image));
        bannerBorder.transform.SetParent(bannerObj.transform, false);
        var bannerBorderRt = bannerBorder.GetComponent<RectTransform>();
        bannerBorderRt.anchorMin = Vector2.zero;
        bannerBorderRt.anchorMax = Vector2.one;
        bannerBorderRt.sizeDelta = new Vector2(-8, -8);
        var bannerBorderImg = bannerBorder.GetComponent<Image>();
        bannerBorderImg.color = new Color(0.3f, 0.1f, 0.6f, 0.5f);
        bannerBorderImg.raycastTarget = false;

        GameObject bannerTextObj = CreateTextAtTransform(bannerObj.transform, "WAVE 1", 72, TextAlignmentOptions.Midline,
            new Color(0.5f, 0.7f, 1f), font);
        bannerTextObj.GetComponent<RectTransform>().sizeDelta = new Vector2(680, 120);
        var bannerTmp = bannerTextObj.GetComponent<TextMeshProUGUI>();
        bannerTmp.fontStyle = FontStyles.Bold;

        uiManager.waveBanner = bannerObj;
        bannerObj.SetActive(false);
        uiManager.waveBannerText = bannerTmp;

        // ===== Wave Clear Banner =====
        GameObject waveClearObj = new GameObject("WaveClearBanner", typeof(Image));
        waveClearObj.transform.SetParent(canvasObj.transform, false);
        var wcRt = waveClearObj.GetComponent<RectTransform>();
        wcRt.anchorMin = new Vector2(0.5f, 0.5f);
        wcRt.anchorMax = new Vector2(0.5f, 0.5f);
        wcRt.sizeDelta = new Vector2(550, 100);
        wcRt.anchoredPosition = new Vector2(0, 60);

        var wcImage = waveClearObj.GetComponent<Image>();
        wcImage.color = new Color(0f, 0.25f, 0.15f, 0.85f);

        GameObject wcBorder = new GameObject("WCBorder", typeof(Image));
        wcBorder.transform.SetParent(waveClearObj.transform, false);
        var wcBorderRt = wcBorder.GetComponent<RectTransform>();
        wcBorderRt.anchorMin = Vector2.zero;
        wcBorderRt.anchorMax = Vector2.one;
        wcBorderRt.sizeDelta = new Vector2(-8, -8);
        var wcBorderImg = wcBorder.GetComponent<Image>();
        wcBorderImg.color = new Color(0f, 0.5f, 0.3f, 0.4f);
        wcBorderImg.raycastTarget = false;

        GameObject wcTextObj = CreateTextAtTransform(waveClearObj.transform, "WAVE CLEARED!", 56,
            TextAlignmentOptions.Midline, new Color(0.3f, 1f, 0.5f), font);
        wcTextObj.GetComponent<RectTransform>().sizeDelta = new Vector2(520, 80);
        var wcTmp = wcTextObj.GetComponent<TextMeshProUGUI>();
        wcTmp.fontStyle = FontStyles.Bold;

        uiManager.waveClearBanner = waveClearObj;
        uiManager.waveClearText = wcTmp;
        waveClearObj.SetActive(false);

        // ===== Boss Health Bar =====
        // Semi-transparent background for boss bar area
        GameObject bossPanel = new GameObject("BossPanel", typeof(Image));
        bossPanel.transform.SetParent(canvasObj.transform, false);
        var bossPanelRt = bossPanel.GetComponent<RectTransform>();
        bossPanelRt.anchorMin = new Vector2(0.5f, 1);
        bossPanelRt.anchorMax = new Vector2(0.5f, 1);
        bossPanelRt.pivot = new Vector2(0.5f, 1);
        bossPanelRt.anchoredPosition = new Vector2(0, -16);
        bossPanelRt.sizeDelta = new Vector2(560, 60);
        var bossPanelImg = bossPanel.GetComponent<Image>();
        bossPanelImg.color = new Color(0, 0, 0, 0.4f);
        bossPanelImg.raycastTarget = false;

        GameObject bossBarObj = CreateSlider(bossPanel.transform, "BossHealthBar", new Vector2(10, -10),
            new Vector2(540, 22), new Color(0.8f, 0.2f, 0.6f), new Color(0.2f, 0.05f, 0.2f, 0.6f),
            out uiManager.bossHealthBarFillRt);
        uiManager.bossHealthBarFillImage = uiManager.bossHealthBarFillRt.GetComponent<Image>();

        GameObject bossNameObj = CreateTextAtTransform(bossPanel.transform, "BOSS", 18, TextAlignmentOptions.Midline,
            new Color(0.8f, 0.2f, 0.6f), font);
        bossNameObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -36);
        bossNameObj.GetComponent<RectTransform>().sizeDelta = new Vector2(540, 24);
        uiManager.bossNameText = bossNameObj.GetComponent<TMP_Text>();

        uiManager.bossHealthBarObj = bossPanel;
        bossPanel.SetActive(false);

        // ===== Level Up Panel =====
        GameObject lvlUpPanel = new GameObject("LevelUpPanel", typeof(Image));
        lvlUpPanel.transform.SetParent(canvasObj.transform, false);
        var lvlUpRt = lvlUpPanel.GetComponent<RectTransform>();
        lvlUpRt.anchorMin = Vector2.zero;
        lvlUpRt.anchorMax = Vector2.one;
        lvlUpRt.sizeDelta = Vector2.zero;

        var lvlUpBg = lvlUpPanel.GetComponent<Image>();
        lvlUpBg.color = new Color(0, 0, 0, 0.75f);

        // Title
        GameObject lvlTitleObj = CreateTextAtTransform(lvlUpPanel.transform, "LEVEL UP!", 60, TextAlignmentOptions.Top,
            new Color(1f, 0.8f, 0.2f), font);
        var lvlTitleRt = lvlTitleObj.GetComponent<RectTransform>();
        lvlTitleRt.anchoredPosition = new Vector2(0, 220);
        lvlTitleRt.sizeDelta = new Vector2(600, 80);
        var lvlTitleTmp = lvlTitleObj.GetComponent<TextMeshProUGUI>();
        lvlTitleTmp.fontStyle = FontStyles.Bold;
        lvlTitleTmp.outlineWidth = 0.2f;
        lvlTitleTmp.outlineColor = new Color(0.5f, 0.3f, 0f);
        uiManager.levelUpTitleText = lvlTitleTmp;

        // "Choose an upgrade" subtitle
        GameObject lvlSubObj = CreateTextAtTransform(lvlUpPanel.transform, "Choose an upgrade", 28,
            TextAlignmentOptions.Top, new Color(0.8f, 0.8f, 0.8f), font);
        lvlSubObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 170);
        lvlSubObj.GetComponent<RectTransform>().sizeDelta = new Vector2(500, 36);

        // Card container
        GameObject cardContainer = new GameObject("UpgradeCardContainer");
        cardContainer.transform.SetParent(lvlUpPanel.transform, false);
        var containerRt = cardContainer.AddComponent<RectTransform>();
        containerRt.anchorMin = new Vector2(0.5f, 0.5f);
        containerRt.anchorMax = new Vector2(0.5f, 0.5f);
        containerRt.sizeDelta = new Vector2(960, 380);
        containerRt.anchoredPosition = new Vector2(0, -20);

        var gridLayout = cardContainer.AddComponent<GridLayoutGroup>();
        gridLayout.cellSize = new Vector2(280, 340);
        gridLayout.spacing = new Vector2(30, 0);
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = 3;
        gridLayout.childAlignment = TextAnchor.MiddleCenter;

        uiManager.levelUpPanel = lvlUpPanel;
        lvlUpPanel.SetActive(false);
        uiManager.upgradeCardContainer = cardContainer.transform;

        // Create upgrade card prefab
        GameObject cardPrefab = CreateUpgradeCardPrefab(font);
        uiManager.upgradeCardPrefab = cardPrefab;

        // ===== Game Over Panel =====
        GameObject goPanel = new GameObject("GameOverPanel", typeof(Image));
        goPanel.SetActive(false);
        goPanel.transform.SetParent(canvasObj.transform, false);
        var goRt = goPanel.GetComponent<RectTransform>();
        goRt.anchorMin = Vector2.zero;
        goRt.anchorMax = Vector2.one;
        goRt.sizeDelta = Vector2.zero;

        var goBg = goPanel.GetComponent<Image>();
        goBg.color = new Color(0, 0, 0, 0.85f);
        goBg.raycastTarget = true;

        // Game Over title
        GameObject goTitle = CreateTextAtTransform(goPanel.transform, "GAME OVER", 80, TextAlignmentOptions.Top,
            new Color(1f, 0.15f, 0.15f), font);
        goTitle.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 250);
        goTitle.GetComponent<RectTransform>().sizeDelta = new Vector2(600, 110);
        var goTitleTmp = goTitle.GetComponent<TextMeshProUGUI>();
        goTitleTmp.fontStyle = FontStyles.Bold;
        goTitleTmp.outlineWidth = 0.3f;
        goTitleTmp.outlineColor = new Color(0.3f, 0f, 0f);
        uiManager.gameOverTitleText = goTitleTmp;

        // Decorative separator line
        GameObject goSep = new GameObject("GoSep", typeof(Image));
        goSep.transform.SetParent(goPanel.transform, false);
        var goSepRt = goSep.GetComponent<RectTransform>();
        goSepRt.anchorMin = new Vector2(0.5f, 0.5f);
        goSepRt.anchorMax = new Vector2(0.5f, 0.5f);
        goSepRt.sizeDelta = new Vector2(300, 2);
        goSepRt.anchoredPosition = new Vector2(0, 130);
        var goSepImg = goSep.GetComponent<Image>();
        goSepImg.color = new Color(1f, 1f, 1f, 0.2f);

        // Stats
        GameObject goScore = CreateTextAtTransform(goPanel.transform, "Score: 0", 42, TextAlignmentOptions.Midline,
            Color.white, font);
        goScore.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 90);
        goScore.GetComponent<RectTransform>().sizeDelta = new Vector2(500, 50);
        var goScoreTmp = goScore.GetComponent<TextMeshProUGUI>();
        goScoreTmp.fontStyle = FontStyles.Bold;
        uiManager.finalScoreText = goScoreTmp;

        GameObject goWave = CreateTextAtTransform(goPanel.transform, "Wave Reached: 1", 32, TextAlignmentOptions.Midline,
            new Color(0.6f, 0.6f, 1f), font);
        goWave.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 35);
        goWave.GetComponent<RectTransform>().sizeDelta = new Vector2(500, 40);
        uiManager.finalWaveText = goWave.GetComponent<TMP_Text>();

        GameObject goTime = CreateTextAtTransform(goPanel.transform, "Time: 00:00", 28, TextAlignmentOptions.Midline,
            new Color(0.7f, 0.7f, 0.7f), font);
        goTime.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -5);
        goTime.GetComponent<RectTransform>().sizeDelta = new Vector2(500, 36);
        uiManager.finalTimeText = goTime.GetComponent<TMP_Text>();

        // High score
        GameObject goHS = CreateTextAtTransform(goPanel.transform, "High Score: 0", 32, TextAlignmentOptions.Midline,
            new Color(1f, 0.8f, 0.2f), font);
        goHS.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -55);
        goHS.GetComponent<RectTransform>().sizeDelta = new Vector2(500, 40);
        var goHSTmp = goHS.GetComponent<TextMeshProUGUI>();
        goHSTmp.fontStyle = FontStyles.Bold;
        uiManager.highScoreText = goHSTmp;

        // Separator
        GameObject goSep2 = new GameObject("GoSep2", typeof(Image));
        goSep2.transform.SetParent(goPanel.transform, false);
        var goSep2Rt = goSep2.GetComponent<RectTransform>();
        goSep2Rt.anchorMin = new Vector2(0.5f, 0.5f);
        goSep2Rt.anchorMax = new Vector2(0.5f, 0.5f);
        goSep2Rt.sizeDelta = new Vector2(300, 2);
        goSep2Rt.anchoredPosition = new Vector2(0, -100);
        var goSep2Img = goSep2.GetComponent<Image>();
        goSep2Img.color = new Color(1f, 1f, 1f, 0.2f);

        // Restart Button
        CreateButton(goPanel.transform, "RESTART", new Vector2(0, -150), new Vector2(340, 65),
            new Color(0.15f, 0.55f, 0.15f), () => uiManager.RestartGame(), font);

        // Quit Button
        CreateButton(goPanel.transform, "QUIT", new Vector2(0, -240), new Vector2(340, 65),
            new Color(0.55f, 0.15f, 0.15f), () => uiManager.QuitGame(), font);

        uiManager.gameOverPanel = goPanel;

        // ===== Pause Panel =====
        GameObject pausePanel = new GameObject("PausePanel", typeof(Image));
        pausePanel.SetActive(false);
        pausePanel.transform.SetParent(canvasObj.transform, false);
        var pauseRt = pausePanel.GetComponent<RectTransform>();
        pauseRt.anchorMin = Vector2.zero;
        pauseRt.anchorMax = Vector2.one;
        pauseRt.sizeDelta = Vector2.zero;

        var pauseBg = pausePanel.GetComponent<Image>();
        pauseBg.color = new Color(0, 0, 0, 0.6f);

        // Pause title
        GameObject pauseTitle = CreateTextAtTransform(pausePanel.transform, "PAUSED", 72, TextAlignmentOptions.Midline,
            new Color(0.6f, 0.7f, 1f), font);
        pauseTitle.GetComponent<RectTransform>().sizeDelta = new Vector2(500, 100);
        var pauseTmp = pauseTitle.GetComponent<TextMeshProUGUI>();
        pauseTmp.fontStyle = FontStyles.Bold;
        pauseTmp.outlineWidth = 0.2f;
        pauseTmp.outlineColor = new Color(0.1f, 0.1f, 0.3f);

        // Resume text
        GameObject resumeObj = CreateTextAtTransform(pausePanel.transform, "Press ESC to resume", 28,
            TextAlignmentOptions.Midline, new Color(0.6f, 0.6f, 0.6f), font);
        resumeObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -60);
        resumeObj.GetComponent<RectTransform>().sizeDelta = new Vector2(500, 40);

        uiManager.pausePanel = pausePanel;

        // ===== Title Screen =====
        GameObject titleObj = new GameObject("TitleScreen", typeof(Image));
        titleObj.transform.SetParent(canvasObj.transform, false);
        var titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.anchorMin = Vector2.zero;
        titleRt.anchorMax = Vector2.one;
        titleRt.sizeDelta = Vector2.zero;

        var titleBg = titleObj.GetComponent<Image>();
        titleBg.color = new Color(0.02f, 0.02f, 0.06f, 1f);

        // Game title
        GameObject gameTitle = CreateTextAtTransform(titleObj.transform, "T R S T", 96, TextAlignmentOptions.Midline,
            new Color(0.2f, 0.8f, 1f), font);
        gameTitle.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 80);
        gameTitle.GetComponent<RectTransform>().sizeDelta = new Vector2(800, 130);
        var titleTmp = gameTitle.GetComponent<TextMeshProUGUI>();
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.outlineWidth = 0.3f;
        titleTmp.outlineColor = new Color(0f, 0.3f, 0.5f);

        // Subtitle
        GameObject subtitle = CreateTextAtTransform(titleObj.transform, "Survive the waves", 28,
            TextAlignmentOptions.Midline, new Color(0.5f, 0.7f, 1f, 0.7f), font);
        subtitle.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 20);
        subtitle.GetComponent<RectTransform>().sizeDelta = new Vector2(600, 40);

        // Press any key
        GameObject pressKey = CreateTextAtTransform(titleObj.transform, "PRESS ANY KEY TO START", 24,
            TextAlignmentOptions.Midline, new Color(0.6f, 0.6f, 0.6f), font);
        pressKey.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -80);
        pressKey.GetComponent<RectTransform>().sizeDelta = new Vector2(500, 40);
        uiManager.titlePressKeyText = pressKey.GetComponent<TMP_Text>();

        // Controls hint
        GameObject controlsHintObj = CreateTextAtTransform(titleObj.transform,
            "WASD: Move  |  Mouse: Aim & Shoot  |  Shift: Sprint  |  Space: Dash  |  ESC: Pause",
            18, TextAlignmentOptions.Midline, new Color(0.4f, 0.4f, 0.4f, 0.6f), font);
        controlsHintObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -180);
        controlsHintObj.GetComponent<RectTransform>().sizeDelta = new Vector2(800, 30);

        // Created by attribution
        GameObject creditObj = CreateTextAtTransform(titleObj.transform,
            "Made by Claude — trst.systems", 14,
            TextAlignmentOptions.Bottom, new Color(0.3f, 0.3f, 0.3f, 0.5f), font);
        creditObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -520);
        creditObj.GetComponent<RectTransform>().sizeDelta = new Vector2(500, 24);

        uiManager.titleScreen = titleObj;
        titleObj.SetActive(false);

        // Show title screen by default
        uiManager.ShowTitleScreen();
    }

    /// <summary>
    /// Creates a manual bar (background + fill image). No Slider component —
    /// UIManager drives the fill by changing the fill RectTransform's anchorMax.x.
    /// </summary>
    GameObject CreateSlider(Transform parent, string name, Vector2 anchoredPos, Vector2 sizeDelta,
        Color fillColor, Color bgColor, out RectTransform fillRt)
    {
        // Container (background)
        GameObject barObj = new GameObject(name);
        barObj.transform.SetParent(parent, false);
        var rt = barObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;

        var bgImage = barObj.AddComponent<Image>();
        bgImage.color = bgColor;

        // Fill (child of container, stretches to fill with margins)
        GameObject fillObj = new GameObject("Fill", typeof(Image));
        fillObj.transform.SetParent(barObj.transform, false);
        fillRt = fillObj.GetComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = new Vector2(2, 2);
        fillRt.offsetMax = new Vector2(-2, -2);
        var fillImage = fillObj.GetComponent<Image>();
        fillImage.color = fillColor;
        fillImage.type = Image.Type.Simple;

        return barObj;
    }

    /// <summary>
    /// Creates a TextMeshProUGUI as a child of parent rect.
    /// Adds the TextMeshProUGUI component directly (not the abstract TMP_Text).
    /// </summary>
    GameObject CreateTextAtParent(Transform parent, string name, Vector2 anchoredPos, string text,
        int fontSize, TextAlignmentOptions alignment, Color color, TMP_FontAsset font,
        bool centerH = false, bool rightH = false)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.SetParent(parent, false);

        var rt = textObj.AddComponent<RectTransform>();

        if (centerH)
        {
            rt.anchorMin = new Vector2(0.5f, 1);
            rt.anchorMax = new Vector2(0.5f, 1);
            rt.pivot = new Vector2(0.5f, 1);
        }
        else if (rightH)
        {
            rt.anchorMin = new Vector2(1, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(1, 1);
        }
        else
        {
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
        }

        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = new Vector2(400, 40);

        var tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = alignment;
        if (font != null) tmp.font = font;

        return textObj;
    }

    /// <summary>
    /// Creates a TextMeshProUGUI centered on a transform (no repositioning).
    /// </summary>
    GameObject CreateTextAtTransform(Transform parent, string text, int fontSize,
        TextAlignmentOptions alignment, Color color, TMP_FontAsset font)
    {
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(parent, false);

        var rt = textObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(400, 60);
        rt.anchoredPosition = Vector2.zero;

        var tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = alignment;
        if (font != null) tmp.font = font;

        return textObj;
    }

    void CreateButton(Transform parent, string label, Vector2 anchoredPos, Vector2 size,
        Color color, UnityAction onClick, TMP_FontAsset font)
    {
        GameObject btnObj = new GameObject($"Btn_{label}");
        btnObj.transform.SetParent(parent, false);
        var rt = btnObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = anchoredPos;

        var img = btnObj.AddComponent<Image>();
        img.color = color;

        var btn = btnObj.AddComponent<Button>();

        // Add hover effect
        var colors = btn.colors;
        colors.highlightedColor = new Color(color.r * 1.4f, color.g * 1.4f, color.b * 1.4f, 1);
        colors.pressedColor = color * 0.7f;
        colors.selectedColor = color;
        colors.normalColor = color;
        btn.colors = colors;

        btn.onClick.AddListener(onClick);

        // Text must be a child GameObject (Image and TextMeshProUGUI both inherit Graphic)
        GameObject txtObj = new GameObject("ButtonText");
        txtObj.transform.SetParent(btnObj.transform, false);
        var txtRt = txtObj.AddComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.sizeDelta = Vector2.zero;

        var btnText = txtObj.AddComponent<TextMeshProUGUI>();
        btnText.text = label;
        btnText.fontSize = 28;
        btnText.color = Color.white;
        btnText.alignment = TextAlignmentOptions.Midline;
        btnText.fontStyle = FontStyles.Bold;
        if (font != null) btnText.font = font;
    }

    GameObject CreateUpgradeCardPrefab(TMP_FontAsset font)
    {
        GameObject card = new GameObject("UpgradeCard");
        card.SetActive(false);

        var cardRt = card.AddComponent<RectTransform>();
        cardRt.sizeDelta = new Vector2(260, 320);

        var cardImage = card.AddComponent<Image>();
        cardImage.color = new Color(0.15f, 0.15f, 0.25f, 0.9f);

        var btn = card.AddComponent<Button>();

        // Name text
        GameObject nameObj = CreateTextAtTransform(card.transform, "Upgrade", 26, TextAlignmentOptions.Top,
            Color.white, font);
        nameObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 80);
        nameObj.GetComponent<RectTransform>().sizeDelta = new Vector2(240, 40);

        // Rarity text
        GameObject rarityObj = CreateTextAtTransform(card.transform, "Common", 16, TextAlignmentOptions.Bottom,
            new Color(0.6f, 0.6f, 0.6f), font);
        rarityObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -120);
        rarityObj.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 30);

        // Description text
        GameObject descObj = CreateTextAtTransform(card.transform, "Description", 20, TextAlignmentOptions.Midline,
            new Color(0.8f, 0.8f, 0.8f), font);
        descObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -20);
        descObj.GetComponent<RectTransform>().sizeDelta = new Vector2(220, 100);

        var cardUI = card.AddComponent<UpgradeCardUI>();
        cardUI.nameText = nameObj.GetComponent<TextMeshProUGUI>();
        cardUI.descriptionText = descObj.GetComponent<TextMeshProUGUI>();
        cardUI.backgroundImage = cardImage;

        return card;
    }

    void CreateAudio()
    {
        GameObject audioObj = new GameObject("AudioManager");
        var audioManager = audioObj.AddComponent<AudioManager>();
        Track(audioObj);

        audioManager.sfxSource = audioObj.AddComponent<AudioSource>();
        audioManager.sfxSource.playOnAwake = false;
        audioManager.sfxSource.volume = 0.5f;

        audioManager.musicSource = audioObj.AddComponent<AudioSource>();
        audioManager.musicSource.loop = true;
        audioManager.musicSource.volume = 0.3f;

        // Create procedural audio clips — richer sounds with harmonics + noise
        audioManager.shootClip = CreateProceduralClip("Shoot", 0.08f, 800, 400, 0.3f, tone: 1f, noise: 0.4f, harmonic: 2f);
        audioManager.hitClip = CreateProceduralClip("Hit", 0.12f, 200, 80, 0.5f, tone: 0.5f, noise: 0.6f);
        audioManager.hitmarkerClip = CreateProceduralClip("Hitmarker", 0.05f, 400, 200, 0.2f, tone: 1f, harmonic: 1.5f);
        audioManager.enemyDeathClip = CreateProceduralClip("EnemyDeath", 0.25f, 400, 60, 0.5f, tone: 0.6f, noise: 0.5f, harmonic: 1.5f);
        audioManager.playerDeathClip = CreateProceduralClip("PlayerDeath", 0.6f, 200, 30, 0.7f, tone: 0.8f, noise: 0.3f, harmonic: 0.5f);
        audioManager.levelUpClip = CreateProceduralClip("LevelUp", 0.4f, 400, 1200, 0.6f, tone: 1f, harmonic: 2f);
        audioManager.upgradeClip = CreateProceduralClip("Upgrade", 0.2f, 600, 900, 0.5f, tone: 1f, harmonic: 1.5f);
        audioManager.waveStartClip = CreateProceduralClip("WaveStart", 0.5f, 300, 600, 0.6f, tone: 1f, harmonic: 2.5f, noise: 0.2f);
        audioManager.dashClip = CreateProceduralClip("Dash", 0.15f, 200, 800, 0.4f, tone: 1f, harmonic: 3f, noise: 0.3f);

        // Procedural background music — ambient drone with slow chord movement
        audioManager.gameplayMusic = CreateMusicClip("GameplayMusic", 16f);
    }

    AudioClip CreateMusicClip(string name, float duration)
    {
        int sampleRate = 44100;
        int samples = Mathf.RoundToInt(sampleRate * duration);
        float[] waveform = new float[samples];

        // Chord frequencies (A minor-ish, slow shifting)
        float[][] chords = new float[][]
        {
            new float[] { 110f, 130.81f, 164.81f }, // Am
            new float[] { 98f, 110f, 130.81f },      // G
            new float[] { 130.81f, 164.81f, 196f },  // C
            new float[] { 110f, 146.83f, 196f },     // Dm
        };
        int notesPerChord = samples / chords.Length;

        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)sampleRate;
            int chordIdx = Mathf.Min(i / notesPerChord, chords.Length - 1);
            float[] chord = chords[chordIdx];
            float chordT = (i % notesPerChord) / (float)notesPerChord; // 0-1 within chord

            float sample = 0f;

            // Bass (sine, half volume)
            sample += Mathf.Sin(2 * Mathf.PI * chord[0] * 0.5f * t) * 0.15f;

            // Chord tones (soft saw-like)
            for (int n = 0; n < chord.Length; n++)
            {
                float note = chord[n];
                // Fundamental
                sample += Mathf.Sin(2 * Mathf.PI * note * t) * 0.06f;
                // Soft overtone
                sample += Mathf.Sin(2 * Mathf.PI * note * 2f * t) * 0.03f;
            }

            // Slow LFO pulse (tremolo)
            float lfo = Mathf.Sin(t * 0.5f * Mathf.PI) * 0.3f + 0.7f;
            sample *= lfo;

            // Gentle fade-in/out at chord boundaries to avoid clicks
            float fade = 1f;
            if (chordT < 0.05f) fade = chordT / 0.05f;
            if (chordT > 0.95f) fade = Mathf.Max(0f, (1f - chordT) / 0.05f);

            sample *= fade;

            // Master volume
            sample *= 0.4f;

            waveform[i] = Mathf.Clamp(sample, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create(name, samples, 1, sampleRate, false);
        clip.SetData(waveform, 0);
        return clip;
    }

    AudioClip CreateProceduralClip(string name, float duration, float startFreq, float endFreq,
        float volume, float tone = 1f, float noise = 0f, float harmonic = 0f)
    {
        int sampleRate = 44100;
        int samples = Mathf.RoundToInt(sampleRate * duration);
        float[] waveform = new float[samples];

        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)sampleRate;
            float normT = t / duration;
            float freq = Mathf.Lerp(startFreq, endFreq, normT);

            // Fundamental sine wave
            float sample = Mathf.Sin(2 * Mathf.PI * freq * t) * tone;

            // Harmonic (octave above for richness)
            if (harmonic > 0)
                sample += Mathf.Sin(2 * Mathf.PI * freq * harmonic * t) * tone * 0.4f;

            // White noise for impact "crunch"
            if (noise > 0)
                sample += (Random.value * 2f - 1f) * noise * (1f - normT * 0.8f);

            // Envelope
            float envelope = 1f;
            if (t < 0.01f) envelope = t / 0.01f;
            if (t > duration - 0.05f) envelope = Mathf.Max(0, (duration - t) / 0.05f);

            waveform[i] = Mathf.Clamp(sample * envelope * volume, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create(name, samples, 1, sampleRate, false);
        clip.SetData(waveform, 0);
        return clip;
    }

    void CreateVFX()
    {
        GameObject vfxObj = new GameObject("VFXManager");
        vfxObj.AddComponent<VFXManager>();
        Track(vfxObj);
    }

    void CreateCameraFollow()
    {
        Camera cam = Camera.main;
        if (cam != null) Track(cam.gameObject);
        if (cam == null)
        {
            GameObject camObj = new GameObject("MainCamera", typeof(Camera), typeof(AudioListener));
            camObj.tag = "MainCamera"; // Camera.main searches by tag
            cam = camObj.GetComponent<Camera>();
        }

        // Always configure camera fully, whether it was the scene camera or a new one
        cam.transform.position = new Vector3(0, 0, -10);
        cam.orthographic = true;
        cam.orthographicSize = 10f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.02f, 0.01f, 0.05f);
        cam.nearClipPlane = 0.01f;
        cam.farClipPlane = 100f;

        var cf = cam.GetComponent<CameraFollow>();
        if (cf == null) cf = cam.gameObject.AddComponent<CameraFollow>();
        cf.target = null;
        cf.orthographicSize = 10f;
    }

    void CreateArena()
    {
        GameObject arenaObj = new GameObject("Arena");
        var init = arenaObj.AddComponent<GameInitializer>();
        Track(arenaObj);
        init.arenaWidth = 32f;
        init.arenaHeight = 24f;
    }
}
