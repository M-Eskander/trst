using UnityEngine;

/// <summary>
/// Boss enemy that appears every 5 waves.
/// Comes in two variations chosen randomly:
///   Ranged   — shoots burst projectiles, phases change behavior
///   Berserker — charges at the player, fast, heavy contact damage
/// </summary>
public class BossEnemy : EnemyBase
{
    public enum BossVariation { Ranged, Berserker }

    [Header("Boss Settings")]
    public BossVariation variation;
    public float bossScale = 2.5f;

    [Header("Ranged")]
    public float shootInterval = 1.2f;
    public int burstCount = 3;
    public float burstSpread = 25f;
    public int bossProjectileDamage = 15;
    public float bossProjectileSpeed = 6f;

    [Header("Berserker")]
    public float berserkerSpeed = 3f;
    public float chargeDuration = 1.2f;
    public float chargeCooldown = 2f;
    public float chargeRange = 8f;

    [Header("Boss Phases")]
    public float phaseTwoThreshold = 0.5f;
    public float phaseThreeThreshold = 0.2f;

    private float baseMoveSpeed;
    private Color originalBossColor;
    private float pulseTimer;

    // Ranged-specific
    private float shootTimer;

    // Berserker-specific
    private float chargeTimer;
    private bool isCharging;
    private Vector2 chargeDir;
    private float berserkerBaseSpeed;

    protected override void Start()
    {
        // Override base stats for boss
        maxHealth = 500;
        moveSpeed = 1.2f;
        contactDamage = 20;
        scoreValue = 200;
        xpDropAmount = 100f;
        enemyColor = new Color(0.6f, 0.2f, 0.9f); // Purple

        // Randomly pick a variation
        variation = (BossVariation)Random.Range(0, 2);

        base.Start();

        // Bigger
        transform.localScale = Vector3.one * bossScale;
        baseMoveSpeed = moveSpeed;

        shootTimer = 0.5f;
        originalBossColor = enemyColor;

        // Berserker setup
        berserkerBaseSpeed = berserkerSpeed;
        chargeTimer = chargeCooldown * 0.5f;

        // Color-tint the variation
        if (spriteRenderer != null)
        {
            if (variation == BossVariation.Berserker)
                spriteRenderer.color = new Color(1f, 0.3f, 0.1f); // orange-red
        }

        // Notify UI
        string variantLabel = variation == BossVariation.Berserker ? "BERSERKER" : "RANGED";
        UIManager.Instance?.ShowBossHealthBar(variantLabel);
    }

    protected override void Update()
    {
        base.Update();

        if (player == null || GameManager.Instance.IsGameOver) return;

        if (variation == BossVariation.Ranged)
            UpdateRanged();
        else
            UpdateBerserker();

        // Pulsing glow
        pulseTimer += Time.deltaTime * 3f;
        float pulse = Mathf.Sin(pulseTimer) * 0.15f + 0.85f;

        Color phaseColor = GetPhaseColor();
        if (flashTimer <= 0 && spriteRenderer != null)
        {
            if (variation == BossVariation.Berserker)
                spriteRenderer.color = Color.Lerp(new Color(1f, 0.3f, 0.1f), phaseColor, 0.5f) * pulse;
            else
                spriteRenderer.color = Color.Lerp(enemyColor, phaseColor, 0.5f) * pulse;
        }

        UIManager.Instance?.UpdateBossHealthBar((float)currentHealth / maxHealth);
    }

    void UpdateRanged()
    {
        shootTimer -= Time.deltaTime;
        if (shootTimer <= 0f)
        {
            ShootBurst();
            shootTimer = shootInterval;
        }
    }

    void UpdateBerserker()
    {
        chargeTimer -= Time.deltaTime;

        if (isCharging)
        {
            chargeTimer -= Time.deltaTime;
            if (chargeTimer <= 0f)
            {
                isCharging = false;
                chargeTimer = chargeCooldown;
            }
        }
        else if (chargeTimer <= 0f &&
                 Vector2.Distance(transform.position, player.position) < chargeRange)
        {
            // Start charge
            isCharging = true;
            chargeDir = ((Vector2)player.position - (Vector2)transform.position).normalized;
            chargeTimer = chargeDuration;

            // Windup flash
            if (spriteRenderer != null) spriteRenderer.color = Color.white;
            CameraFollow.Instance?.Shake(0.2f);
        }
    }

    protected override void FixedUpdate()
    {
        if (player == null || GameManager.Instance.IsGameOver)
        {
            if (rb != null)
                rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, 5f * Time.fixedDeltaTime);
            return;
        }

        float healthPercent = (float)currentHealth / maxHealth;
        float speedMult = healthPercent < phaseThreeThreshold ? 1.6f :
                          healthPercent < phaseTwoThreshold ? 1.3f : 1f;

        if (variation == BossVariation.Ranged)
        {
            Vector2 dir = ((Vector2)player.position - (Vector2)transform.position).normalized;
            rb.linearVelocity = dir * baseMoveSpeed * speedMult;

            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }
        else // Berserker
        {
            if (isCharging)
            {
                rb.linearVelocity = chargeDir * berserkerBaseSpeed * speedMult * 1.5f;

                // Rotation toward charge direction
                float angle = Mathf.Atan2(chargeDir.y, chargeDir.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0, 0, angle);

                // Screen shake during charge for impact feel
                CameraFollow.Instance?.Shake(0.1f);
            }
            else
            {
                Vector2 dir = ((Vector2)player.position - (Vector2)transform.position).normalized;
                rb.linearVelocity = dir * berserkerBaseSpeed * 0.5f * speedMult;

                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0, 0, angle);
            }
        }
    }

    void ShootBurst()
    {
        if (player == null) return;

        Vector2 toPlayer = ((Vector2)player.position - (Vector2)transform.position).normalized;

        float healthPercent = (float)currentHealth / maxHealth;
        int actualBurst = healthPercent < phaseThreeThreshold ? burstCount + 2 :
                          healthPercent < phaseTwoThreshold ? burstCount + 1 : burstCount;
        float actualSpread = healthPercent < phaseThreeThreshold ? burstSpread * 1.3f :
                             healthPercent < phaseTwoThreshold ? burstSpread * 1.15f : burstSpread;

        for (int i = 0; i < actualBurst; i++)
        {
            float angleOffset = (i - (actualBurst - 1) / 2f) * actualSpread;
            Quaternion rotation = Quaternion.Euler(0, 0,
                Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg + angleOffset);

            EnemyProjectile.Create(transform.position, rotation, bossProjectileSpeed, bossProjectileDamage, GetPhaseColor());
        }

        VFXManager.Instance?.SpawnImpact(transform.position, GetPhaseColor(), 0.4f);
    }

    Color GetPhaseColor()
    {
        float h = (float)currentHealth / maxHealth;
        if (h < phaseThreeThreshold) return Color.red;
        if (h < phaseTwoThreshold) return new Color(1f, 0.5f, 0f);
        if (variation == BossVariation.Berserker) return new Color(1f, 0.3f, 0.1f);
        return new Color(0.6f, 0.2f, 0.9f);
    }

    public override void TakeDamage(int amount)
    {
        base.TakeDamage(amount);
        UIManager.Instance?.UpdateBossHealthBar((float)currentHealth / maxHealth);
        CameraFollow.Instance?.Shake(0.15f);
    }

    protected override void Die()
    {
        VFXManager.Instance?.SpawnExplosion(transform.position, Color.yellow, 3f);
        VFXManager.Instance?.SpawnExplosion(transform.position, GetPhaseColor(), 2.5f);
        VFXManager.Instance?.SpawnExplosion(transform.position, Color.white, 2f);
        CameraFollow.Instance?.Shake(1f);

        if (XPOrbPool.Instance != null)
        {
            for (int i = 0; i < 8; i++)
            {
                float burstXP = xpDropAmount * 0.25f;
                Vector2 offset = Random.insideUnitCircle * 1.5f;
                XPOrbPool.Instance.SpawnOrb(transform.position + new Vector3(offset.x, offset.y, 0), burstXP);
            }
        }

        GameManager.Instance.AddScore(scoreValue * 3);
        AudioManager.Instance?.PlayEnemyDeath();

        EnemySpawner.Instance?.OnBossKilled();
        EnemySpawner.Instance?.OnEnemyKilled();

        Destroy(gameObject);
    }

    protected void OnDestroy()
    {
        UIManager.Instance?.HideBossHealthBar();
    }
}
