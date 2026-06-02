using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }

    [Header("Movement")]
    public float baseMoveSpeed = 5f;
    public float moveSpeed;
    public float acceleration = 20f;
    public float deceleration = 18f;

    [Header("Sprint")]
    public float sprintSpeedMultiplier = 1.5f;

    [Header("Dash")]
    public float dashSpeedMultiplier = 5f;
    public float dashDuration = 0.35f;
    public float dashCooldown = 1.5f;

    [Header("Health")]
    public int maxHealth = 100;
    public int currentHealth;
    public float invincibilityTime = 0.3f;
    public float healthRegenRate = 0f; // per second

    [Header("Combat")]
    public int baseDamage = 15;
    public float baseFireRate = 0.2f;
    public int baseProjectileCount = 1;
    public float baseProjectileSpeed = 18f;
    public float baseProjectileSize = 0.35f;

    // Runtime stats (modified by upgrades)
    public int damage { get; set; }
    public float fireRate { get; set; }
    public int projectileCount { get; set; }
    public float projectileSpeed { get; set; }
    public float projectileSize { get; set; }
    public bool piercingShots { get; set; }

    // New upgrade stats
    public float critChance { get; set; }
    public float critMultiplier { get; set; }
    public bool chainLightning { get; set; }
    public float thornsDamage { get; set; }
    public bool frostAura { get; set; }
    public float frostAuraRadius { get; set; }
    public int dashDamage { get; set; }
    public float vampireHealAmount { get; set; }

    [Header("References")]
    public Transform firePoint;
    public SpriteRenderer spriteRenderer;

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Vector2 mousePos;
    private float fireTimer;
    private float invincibilityTimer;
    private bool isDead;

    // Dash
    private bool isDashing;
    private float dashTimer;
    private float dashCooldownTimer;

    // Sprint
    private bool isSprinting;

    // Health regen (fractional accumulator to avoid rounding-to-zero per frame)
    private float healthRegenAccumulator;

    void Awake()
    {
        Instance = this;
        rb = GetComponent<Rigidbody2D>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        currentHealth = maxHealth;
        moveSpeed = baseMoveSpeed;
        ResetCombatStats();
    }

    public void ResetCombatStats()
    {
        damage = baseDamage;
        fireRate = baseFireRate;
        projectileCount = baseProjectileCount;
        projectileSpeed = baseProjectileSpeed;
        projectileSize = baseProjectileSize;
        piercingShots = false;
        critChance = 0f;
        critMultiplier = 2f;
        chainLightning = false;
        thornsDamage = 0f;
        frostAura = false;
        frostAuraRadius = 3f;
        dashDamage = 0;
        vampireHealAmount = 0f;
    }

    void Update()
    {
        if (GameManager.Instance.IsGameOver) return;

        // Dash timer
        if (isDashing)
        {
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0f)
            {
                isDashing = false;
                if (spriteRenderer != null) spriteRenderer.color = Color.white;
            }
        }
        else
        {
            dashCooldownTimer -= Time.deltaTime;
        }

        // Fire
        fireTimer -= Time.deltaTime;
        if (Mouse.current != null && Mouse.current.leftButton.isPressed && fireTimer <= 0f)
        {
            Fire();
            fireTimer = fireRate;
        }

        // Invincibility
        if (invincibilityTimer > 0)
        {
            invincibilityTimer -= Time.deltaTime;
            // Show sprite fully during first 0.15s (allows dash grace without blink)
            // Then blink at a visible rate for remaining invincibility
            spriteRenderer.enabled = invincibilityTimer > 0.15f || Mathf.Sin(invincibilityTimer * 20f) > 0f;
        }
        else
        {
            spriteRenderer.enabled = true;
        }

        // Health regen (accumulate fractional HP per frame)
        if (healthRegenRate > 0 && currentHealth < maxHealth)
        {
            healthRegenAccumulator += healthRegenRate * Time.deltaTime;
            if (healthRegenAccumulator >= 1f)
            {
                int healAmount = Mathf.FloorToInt(healthRegenAccumulator);
                currentHealth = Mathf.Min(maxHealth, currentHealth + healAmount);
                healthRegenAccumulator -= healAmount;
                UIManager.Instance?.UpdateHealthBar((float)currentHealth / maxHealth);
            }
        }

        // Frost aura — slow nearby enemies
        if (frostAura)
        {
            var enemies = FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
            foreach (var e in enemies)
            {
                if (e == null) continue;
                float dist = Vector2.Distance(transform.position, e.transform.position);
                if (dist < frostAuraRadius)
                {
                    e.ApplySlow(0.5f, 0.2f); // 50% slow, refreshed each frame
                }
            }
        }
    }

    void FixedUpdate()
    {
        if (GameManager.Instance.IsGameOver)
        {
            rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, deceleration * Time.fixedDeltaTime);
            return;
        }

        float speedMult = isDashing ? dashSpeedMultiplier : (isSprinting ? sprintSpeedMultiplier : 1f);
        Vector2 targetVel = moveInput * moveSpeed * speedMult;
        rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, targetVel, (moveInput.magnitude > 0.1f ? acceleration : deceleration) * Time.fixedDeltaTime);

        // Aim toward mouse
        Vector2 aimDir = (mousePos - (Vector2)transform.position).normalized;
        if (aimDir.magnitude > 0.1f)
        {
            float angle = Mathf.Atan2(aimDir.y, aimDir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }
    }

    void Fire()
    {
        Vector2 aimDir = (mousePos - (Vector2)transform.position).normalized;
        if (aimDir.magnitude < 0.1f) aimDir = Vector2.right;

        float spreadAngle = 15f;
        Vector2 firePos = firePoint != null ? (Vector2)firePoint.position : (Vector2)transform.position;

        for (int i = 0; i < projectileCount; i++)
        {
            float angleOffset = 0f;
            if (projectileCount > 1)
            {
                angleOffset = (i - (projectileCount - 1) / 2f) * spreadAngle;
            }

            Quaternion rotation = Quaternion.Euler(0, 0, Mathf.Atan2(aimDir.y, aimDir.x) * Mathf.Rad2Deg + angleOffset);
            bool isCrit = critChance > 0f && Random.value < critChance;

            Projectile.Create(firePos, rotation, damage, projectileSpeed, projectileSize,
                piercingShots, 10f, isCrit, chainLightning);
        }

        // Muzzle flash / audio
        AudioManager.Instance?.PlayShoot();
    }

    public void TakeDamage(int amount)
    {
        if (invincibilityTimer > 0 || isDead) return;

        currentHealth -= amount;
        invincibilityTimer = invincibilityTime;
        CameraFollow.Instance?.Shake(0.4f);
        VisualEffects.Instance?.DamageFlash();
        AudioManager.Instance?.PlayHit();

        UIManager.Instance?.UpdateHealthBar((float)currentHealth / maxHealth);

        // Thorns — reflect damage back (simple: damage all nearby enemies)
        if (thornsDamage > 0f)
        {
            var enemies = FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
            int thornDmg = Mathf.RoundToInt(amount * thornsDamage);
            foreach (var e in enemies)
            {
                if (e == null) continue;
                float dist = Vector2.Distance(transform.position, e.transform.position);
                if (dist < 3f)
                {
                    e.TakeDamage(thornDmg);
                    VFXManager.Instance?.SpawnImpact(e.transform.position, new Color(0.8f, 0.2f, 0.8f), 0.2f);
                }
            }
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;
        AudioManager.Instance?.PlayDeath();
        VFXManager.Instance?.SpawnExplosion(transform.position, Color.red, 1f);
        GameManager.Instance.GameOver();
    }

    /// <summary>
    /// Called by EnemySpawner.OnEnemyKilled when an enemy dies.
    /// Applies vampire heal if the upgrade has been taken.
    /// </summary>
    public void OnEnemyKilled()
    {
        if (vampireHealAmount > 0f && currentHealth < maxHealth)
        {
            int healAmt = Mathf.RoundToInt(vampireHealAmount);
            Heal(healAmt);
        }
    }

    public void Heal(int amount)
    {
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        UIManager.Instance?.UpdateHealthBar((float)currentHealth / maxHealth);
        VFXManager.Instance?.SpawnHealEffect(transform.position);
    }

    public void Dash()
    {
        if (isDashing || dashCooldownTimer > 0 || isDead) return;

        isDashing = true;
        dashTimer = dashDuration;
        dashCooldownTimer = dashCooldown;
        invincibilityTimer = dashDuration + 0.1f; // brief grace after dash

        // Visual feedback
        if (spriteRenderer != null) spriteRenderer.color = Color.cyan;

        // Initial velocity burst in the direction of movement
        Vector2 burstDir = rb.linearVelocity.normalized;
        if (burstDir.magnitude > 0.1f)
            rb.AddForce(burstDir * 20f, ForceMode2D.Impulse);

        // Speed burst visual effect
        VFXManager.Instance?.SpawnImpact(transform.position, Color.cyan, 0.6f);
        CameraFollow.Instance?.Shake(0.3f);

        // Audio
        AudioManager.Instance?.PlayDash();
    }

    public void SetSprinting(bool sprinting)
    {
        isSprinting = sprinting;
    }

    public void OnMoveInput(Vector2 input) => moveInput = input;
    public void OnAimInput(Vector2 input) => mousePos = input;

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.TryGetComponent<EnemyBase>(out var enemy))
        {
            // Dash damage — hit enemies during dash
            if (isDashing && dashDamage > 0)
            {
                enemy.TakeDamage(dashDamage);
                VFXManager.Instance?.SpawnImpact(collision.contacts[0].point, Color.red, 0.4f);
                AudioManager.Instance?.PlayHitmarker();
                // Blast the enemy away
                Vector2 pushDir = (transform.position - enemy.transform.position).normalized;
                enemy.GetComponent<Rigidbody2D>()?.AddForce(-pushDir * 15f, ForceMode2D.Impulse);
            }

            // Push enemies away slightly on collision
            Vector2 pushDir2 = (transform.position - enemy.transform.position).normalized;
            rb.AddForce(pushDir2 * 5f, ForceMode2D.Impulse);
        }
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
