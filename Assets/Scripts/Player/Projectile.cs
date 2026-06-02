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

    /// <summary>
    /// Factory method — creates a projectile GameObject directly instead of using a prefab.
    /// Avoids Unity 6000 Instantiate-from-inactive-prefab issues.
    /// </summary>
    public static Projectile Create(Vector2 position, Quaternion rotation, int damage, float speed,
        float size, bool piercing, float lifetime, bool crit, bool chain)
    {
        GameObject go = new GameObject("Projectile");
        go.transform.position = new Vector3(position.x, position.y, -1f);
        go.transform.rotation = rotation;

        var sr = go.AddComponent<SpriteRenderer>();
        Texture2D tex = ProceduralAssets.CreateProjectileTexture();
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 16);
        sr.sortingOrder = 15;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var collider = go.AddComponent<CircleCollider2D>();
        collider.radius = 0.15f;
        collider.isTrigger = true;

        var trail = go.AddComponent<TrailRenderer>();
        trail.time = 0.25f;
        trail.startWidth = 0.3f;
        trail.endWidth = 0f;
        var trailMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        if (trailMat != null) trail.material = trailMat;

        var proj = go.AddComponent<Projectile>();
        proj.Initialize(damage, speed, size, piercing, lifetime, crit, chain);

        return proj;
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

        // Apply color to sprite
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = projectileColor;
        }

        // Set velocity immediately (not waiting for Start)
        if (rb != null)
        {
            rb.linearVelocity = transform.right * speed;
        }
    }

    void Start()
    {
        // Legacy — only called if Instantiate-based path is used.
        // Factory method sets velocity in Initialize() so it fires immediately.
        if (rb != null && rb.linearVelocity.magnitude < 0.01f)
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
