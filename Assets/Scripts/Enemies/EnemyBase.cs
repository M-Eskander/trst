using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyBase : MonoBehaviour
{
    [Header("Base Stats")]
    public int maxHealth = 30;
    public float moveSpeed = 2f;
    public int contactDamage = 10;
    public float contactDamageCooldown = 0.5f;
    public int scoreValue = 10;
    public float xpDropAmount = 5f;
    public float damageFlashDuration = 0.1f;

    [Header("References")]
    public Rigidbody2D rb;
    public SpriteRenderer spriteRenderer;
    public Color enemyColor = Color.red;

    protected int currentHealth;
    protected Transform player;
    protected float flashTimer;
    protected float contactDamageTimer;
    protected Color originalColor;

    // Multipliers set by spawner
    [HideInInspector] public float healthMultiplier = 1f;
    [HideInInspector] public float speedMultiplier = 1f;

    protected virtual void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null) originalColor = spriteRenderer.color;
    }

    protected virtual void Start()
    {
        player = PlayerController.Instance?.transform;
        int scaledHealth = Mathf.RoundToInt(maxHealth * healthMultiplier);
        currentHealth = scaledHealth;
        moveSpeed *= speedMultiplier;

        if (spriteRenderer != null)
        {
            spriteRenderer.color = enemyColor;
        }

        StartCoroutine(SpawnAnimation());
    }

    System.Collections.IEnumerator SpawnAnimation()
    {
        // Flash effect on spawn
        VFXManager.Instance?.SpawnImpact(transform.position, enemyColor, 0.3f);

        float t = 0;
        float duration = 0.2f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float scale = Mathf.SmoothStep(0, 1, t / duration);
            transform.localScale = Vector3.one * scale;
            yield return null;
        }
        transform.localScale = Vector3.one;
    }

    protected float slowTimer;
    protected float slowFactor = 1f;

    public void ApplySlow(float factor, float duration)
    {
        slowTimer = Mathf.Max(slowTimer, duration);
        slowFactor = Mathf.Min(slowFactor, factor);
    }

    protected virtual void Update()
    {
        if (flashTimer > 0)
        {
            flashTimer -= Time.deltaTime;
            if (flashTimer <= 0 && spriteRenderer != null)
            {
                spriteRenderer.color = enemyColor;
            }
        }
        if (contactDamageTimer > 0)
        {
            contactDamageTimer -= Time.deltaTime;
        }
        if (slowTimer > 0)
        {
            slowTimer -= Time.deltaTime;
            // Only apply slow tint if not currently flashing from damage
            if (spriteRenderer != null && flashTimer <= 0)
            {
                // Frozen tint
                spriteRenderer.color = Color.Lerp(new Color(0.5f, 0.8f, 1f), enemyColor, 0.4f);
            }
        }
        if (slowTimer <= 0)
        {
            slowFactor = 1f;
        }
    }

    protected virtual void FixedUpdate()
    {
        if (player == null || GameManager.Instance.IsGameOver)
        {
            if (rb != null) rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, 5f * Time.fixedDeltaTime);
            return;
        }

        Vector2 dir = ((Vector2)player.position - (Vector2)transform.position).normalized;
        rb.linearVelocity = dir * moveSpeed * slowFactor;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    public virtual void TakeDamage(int amount)
    {
        currentHealth -= amount;

        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.white;
            flashTimer = damageFlashDuration;
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    protected virtual void Die()
    {
        GameManager.Instance.AddScore(scoreValue);
        CameraFollow.Instance?.Shake(0.2f);
        VFXManager.Instance?.SpawnExplosion(transform.position, enemyColor, 1.2f);

        if (XPOrbPool.Instance != null)
        {
            XPOrbPool.Instance.SpawnOrb(transform.position, xpDropAmount * (1f + GameManager.Instance.GetDifficultyFactor() * 0.5f));
        }

        AudioManager.Instance?.PlayEnemyDeath();
        EnemySpawner.Instance?.OnEnemyKilled();
        Destroy(gameObject);
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        if (contactDamageTimer > 0) return;
        if (collision.gameObject.TryGetComponent<PlayerController>(out var playerScript))
        {
            playerScript.TakeDamage(contactDamage);
            contactDamageTimer = contactDamageCooldown;
        }
    }
}
