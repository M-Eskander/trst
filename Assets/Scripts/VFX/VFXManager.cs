using UnityEngine;
using System.Collections.Generic;

public class VFXManager : MonoBehaviour
{
    public static VFXManager Instance { get; private set; }

    // Cached circle texture — shared across all VFX to avoid creating per-call
    private static Texture2D _circleTex;
    private static Sprite _circleSprite;

    void Awake()
    {
        Instance = this;
        // Pre-create the shared circle texture once
        if (_circleSprite == null)
        {
            _circleTex = new Texture2D(16, 16);
            Color fill = Color.white;
            Color clear = new Color(1, 1, 1, 0);
            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    float dx = x - 7.5f, dy = y - 7.5f;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    _circleTex.SetPixel(x, y, dist < 7f ? fill : clear);
                }
            }
            _circleTex.Apply();
            _circleSprite = Sprite.Create(_circleTex,
                new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16);
        }
    }

    public void SpawnImpact(Vector2 position, Color color, float scale = 0.3f)
    {
        SpawnBurst(position, color, scale, 6, 0.18f, 1f, 0.2f);
    }

    public void SpawnExplosion(Vector2 position, Color color, float scale = 1f)
    {
        SpawnBurst(position, color, scale * 0.6f, 12, 0.4f, 1.8f, 0.35f);
    }

    public void SpawnEnemyDeath(Vector2 position, Color color)
    {
        SpawnBurst(position, color, 0.5f, 8, 0.3f, 1.2f, 0.25f);
    }

    public void SpawnXPPickup(Vector2 position)
    {
        SpawnBurst(position, Color.yellow, 0.15f, 4, 0.15f, 0.5f, 0.12f);
    }

    public void SpawnHealEffect(Vector2 position)
    {
        SpawnBurst(position, Color.green, 0.4f, 8, 0.3f, 1f, 0.2f);
    }

    /// <summary>
    /// Spawn a burst of sprite-based particles that move outward and fade.
    /// Uses SpriteRenderer with default material — guaranteed to render in any build.
    /// </summary>
    void SpawnBurst(Vector2 position, Color color, float scale, int count,
        float lifetime, float speed, float size)
    {
        if (_circleSprite == null) return;

        // For each "particle" in the burst
        for (int i = 0; i < count; i++)
        {
            Vector2 dir = Random.insideUnitCircle.normalized;
            float spd = speed * Random.Range(0.6f, 1.4f);
            float ttl = lifetime * Random.Range(0.7f, 1.3f);
            float sz = size * Random.Range(0.6f, 1.0f);

            GameObject go = new GameObject("VFXParticle");
            go.transform.position = new Vector3(position.x, position.y, -1f);
            go.transform.localScale = Vector3.one * scale * sz;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _circleSprite;
            sr.color = color;
            sr.sortingOrder = 20;

            var mover = go.AddComponent<VFXParticle>();
            mover.Initialize(dir * spd, ttl);
        }
    }
}

/// <summary>
/// Tiny component that moves a VFX sprite outward and destroys it after its lifetime.
/// </summary>
public class VFXParticle : MonoBehaviour
{
    private Vector3 velocity;
    private float lifetime;
    private float timer;
    private SpriteRenderer sr;
    private Color startColor;

    public void Initialize(Vector2 vel, float life)
    {
        velocity = new Vector3(vel.x, vel.y, 0);
        lifetime = life;
        timer = 0f;
        sr = GetComponent<SpriteRenderer>();
        if (sr != null) startColor = sr.color;
    }

    void Update()
    {
        timer += Time.deltaTime;

        // Move
        transform.position += velocity * Time.deltaTime;
        // Friction / slow
        velocity *= 0.95f;

        // Fade out based on remaining lifetime
        float t = timer / lifetime;
        if (t >= 1f)
        {
            Destroy(gameObject);
            return;
        }

        // Shrink gradually
        Vector3 scl = transform.localScale;
        scl.x *= 0.97f;
        scl.y *= 0.97f;
        transform.localScale = scl;

        // Fade alpha
        if (sr != null)
        {
            Color c = startColor;
            c.a = Mathf.Lerp(1f, 0f, t * t); // quadratic fade
            sr.color = c;
        }
    }
}
