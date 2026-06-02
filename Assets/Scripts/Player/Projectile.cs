using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Projectile : MonoBehaviour
{
    private int damage;
    private float speed;
    private float size;
    private bool piercing;
    private float lifetime;
    private float timer;
    private Rigidbody2D rb;
    private TrailRenderer trail;
    private Color projectileColor = Color.cyan;
    private bool isCritical;
    private bool chainLightningEnabled;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        trail = GetComponent<TrailRenderer>();
    }

    public void Initialize(int dmg, float spd, float sz, bool pierce, float life)
    {
        Initialize(dmg, spd, sz, pierce, life, false, false);
    }

    public void Initialize(int dmg, float spd, float sz, bool pierce, float life, bool crit, bool chain)
    {
        damage = dmg;
        speed = spd;
        size = sz;
        piercing = pierce;
        lifetime = life;
        timer = 0f;
        isCritical = crit;
        chainLightningEnabled = chain;

        if (isCritical) damage = Mathf.RoundToInt(damage * PlayerController.Instance.critMultiplier);

        transform.localScale = Vector3.one * Mathf.Clamp(size, 0.1f, 2f);

        if (isCritical)
        {
            projectileColor = new Color(1f, 0.8f, 0f); // gold for crits
            transform.localScale *= 1.3f;
        }
        else
        {
            projectileColor = Color.Lerp(Color.cyan, Color.magenta, Random.value);
        }

        if (trail != null)
        {
            trail.startColor = projectileColor;
            trail.endColor = new Color(projectileColor.r, projectileColor.g, projectileColor.b, 0f);
        }

        // Apply color to sprite or use a colored quad
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = projectileColor;
        }
    }

    void Start()
    {
        if (rb != null)
        {
            rb.linearVelocity = transform.right * speed;
        }
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= lifetime)
        {
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent<EnemyBase>(out var enemy))
        {
            enemy.TakeDamage(damage);
            Color dmgColor = isCritical ? Color.yellow : new Color(1f, 0.5f, 0f);
            VFXManager.Instance?.SpawnImpact(transform.position, isCritical ? new Color(1f, 0.8f, 0f) : projectileColor, 0.3f);
            UIManager.Instance?.SpawnDamageText(transform.position, damage, dmgColor);
            AudioManager.Instance?.PlayHitmarker();

            // Chain lightning to nearby enemies
            if (chainLightningEnabled && !piercing)
            {
                var enemies = FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
                int chained = 0;
                foreach (var e in enemies)
                {
                    if (e == enemy || e == null) continue;
                    float dist = Vector2.Distance(transform.position, e.transform.position);
                    if (dist < 4f && chained < 3)
                    {
                        int chainDmg = Mathf.RoundToInt(damage * 0.5f);
                        e.TakeDamage(chainDmg);
                        VFXManager.Instance?.SpawnImpact(e.transform.position, Color.cyan, 0.2f);
                        UIManager.Instance?.SpawnDamageText(e.transform.position, chainDmg, Color.cyan);
                        chained++;
                    }
                }
            }

            if (!piercing)
            {
                Destroy(gameObject);
            }
        }

        // Destroy on walls/obstacles (tagged "Wall")
        if (other.CompareTag("Wall"))
        {
            VFXManager.Instance?.SpawnImpact(transform.position, projectileColor * 0.5f, 0.2f);
            Destroy(gameObject);
        }
    }
}
