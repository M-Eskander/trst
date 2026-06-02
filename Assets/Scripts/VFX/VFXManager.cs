using UnityEngine;

public class VFXManager : MonoBehaviour
{
    public static VFXManager Instance { get; private set; }

    void Awake()
    {
        Instance = this;
    }

    public void SpawnImpact(Vector2 position, Color color, float scale = 0.3f)
    {
        SpawnParticle(position, color, scale, 0.2f, 1, 6, 0.2f);
    }

    public void SpawnExplosion(Vector2 position, Color color, float scale = 1f)
    {
        SpawnParticle(position, color, scale, 0.5f, 2, 12, 0.4f);
    }

    public void SpawnEnemyDeath(Vector2 position, Color color)
    {
        SpawnParticle(position, color, 0.6f, 0.3f, 1.5f, 8, 0.3f);
    }

    public void SpawnXPPickup(Vector2 position)
    {
        SpawnParticle(position, Color.yellow, 0.2f, 0.15f, 1f, 4, 0.15f);
    }

    public void SpawnHealEffect(Vector2 position)
    {
        SpawnParticle(position, Color.green, 0.5f, 0.3f, 1f, 8, 0.25f);
    }

    void SpawnParticle(Vector2 position, Color color, float scale, float duration,
        float speed, int burstCount, float size)
    {
        // Create inactive so AddComponent can't auto-play while we configure
        GameObject go = new GameObject("VFX");
        go.SetActive(false);
        go.transform.position = new Vector3(position.x, position.y, -1f);
        go.transform.localScale = Vector3.one * scale;

        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = duration;
        main.startLifetime = duration;
        main.startSpeed = speed;
        main.startSize = size;
        main.startColor = color;
        main.loop = false;
        main.maxParticles = Mathf.Max(burstCount * 2, 10);
        main.playOnAwake = false;

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[]
        {
            new ParticleSystem.Burst(0, new ParticleSystem.MinMaxCurve(burstCount))
        });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.3f;

        // Use URP particles unlit shader — ensures particles render in builds
        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        Material particleMat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        if (particleMat != null)
        {
            particleMat.color = color;
            renderer.material = particleMat;
        }
        else
        {
            renderer.material = null; // fallback to default
        }

        go.SetActive(true);
        ps.Play();
        Destroy(go, duration + 0.5f);
    }
}
