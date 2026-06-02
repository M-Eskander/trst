using UnityEngine;

/// <summary>
/// Simple projectile fired by boss enemies toward the player.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyProjectile : MonoBehaviour
{
    public float speed = 5f;
    public int damage = 15;
    public float lifetime = 5f;
    public Color projectileColor = new Color(0.8f, 0.2f, 0.6f);

    private Rigidbody2D rb;
    private Vector2 direction;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public static EnemyProjectile Create(Vector2 position, Quaternion rotation, float speed, int damage, Color color)
    {
        GameObject go = new GameObject("EnemyProjectile");
        go.transform.position = position;
        go.transform.rotation = rotation;

        var sr = go.AddComponent<SpriteRenderer>();
        Texture2D tex = ProceduralAssets.CreateProjectileTexture();
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 16);
        sr.color = color;
        sr.sortingOrder = 14;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0;

        var collider = go.AddComponent<CircleCollider2D>();
        collider.radius = 0.15f;
        collider.isTrigger = true;

        var ep = go.AddComponent<EnemyProjectile>();
        ep.Initialize(rotation * Vector2.right, speed, damage, color);

        return ep;
    }

    public void Initialize(Vector2 dir, float spd, int dmg, Color color)
    {
        direction = dir.normalized;
        speed = spd;
        damage = dmg;
        projectileColor = color;

        // Set rotation to face movement direction
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        // Apply velocity
        if (rb != null)
        {
            rb.linearVelocity = direction * speed;
        }

        // Set sprite color
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = color;

        Destroy(gameObject, lifetime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent<PlayerController>(out var player))
        {
            player.TakeDamage(damage);
            VFXManager.Instance?.SpawnImpact(transform.position, projectileColor, 0.25f);
            Destroy(gameObject);
        }

        // Destroy on wall/obstacle hit (objects with "Wall" tag)
        if (other.CompareTag("Wall"))
        {
            VFXManager.Instance?.SpawnImpact(transform.position, projectileColor * 0.5f, 0.15f);
            Destroy(gameObject);
        }
    }
}
