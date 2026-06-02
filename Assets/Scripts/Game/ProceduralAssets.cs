using UnityEngine;

/// <summary>
/// Creates procedural textures and materials at runtime so no external art assets are needed.
/// </summary>
public static class ProceduralAssets
{
    static Shader GetDefaultShader()
    {
        // Try shaders in order of preference for URP sprite rendering
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        if (shader == null) shader = Shader.Find("Standard");
        return shader;
    }

    public static Material CreateNeonMaterial(Color color, bool glow = true)
    {
        Shader shader = GetDefaultShader();
        if (shader == null)
        {
            Debug.LogError("[ProceduralAssets] No shader found! Cannot create materials.");
            return null;
        }
        Material mat = new Material(shader);

        mat.color = color;

        if (glow && mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * 0.5f);
        }

        return mat;
    }

    public static Texture2D CreateCircleTexture(int radius, Color color)
    {
        int size = radius * 2;
        Texture2D tex = new Texture2D(size, size);
        Color clear = Color.clear;
        Color fill = color;
        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int dx = x - radius;
                int dy = y - radius;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                pixels[y * size + x] = dist <= radius ? fill : clear;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    /// <summary>
    /// Creates a simple spaceship-shaped sprite texture procedurally.
    /// </summary>
    public static Texture2D CreatePlayerTexture()
    {
        int size = 64;
        Texture2D tex = new Texture2D(size, size);
        Color clear = Color.clear;
        Color white = Color.white;
        Color engine = new Color(0.3f, 0.6f, 1f);
        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float nx = (x / (float)size) * 2f - 1f;
                float ny = (y / (float)size) * 2f - 1f;

                bool inside = false;

                // Main body - elongated hexagon/diamond
                if (nx > -0.2f && Mathf.Abs(ny) < 0.5f * (1f - nx * 0.2f))
                    inside = true;
                // Nose
                if (nx > 0.4f && Mathf.Abs(ny) < 0.15f)
                    inside = true;

                // Engine glow at back
                if (nx < -0.3f && Mathf.Abs(ny) < 0.12f)
                {
                    pixels[y * size + x] = engine;
                    continue;
                }

                // Wing tips
                if (nx > -0.4f && nx < 0.2f && Mathf.Abs(ny) > 0.45f && Mathf.Abs(ny) < 0.65f)
                    inside = true;

                pixels[y * size + x] = inside ? white : clear;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    public static Texture2D CreateEnemyTexture(EnemyType type)
    {
        int size = 64;
        Texture2D tex = new Texture2D(size, size);
        Color clear = Color.clear;
        Color fill = Color.white;
        Color inner = new Color(0.7f, 0.7f, 0.7f);
        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float nx = (x / (float)size) * 2f - 1f;
                float ny = (y / (float)size) * 2f - 1f;
                Color pixel = clear;

                switch (type)
                {
                    case EnemyType.Basic:
                        // Diamond with inner diamond detail
                        if (Mathf.Abs(nx) + Mathf.Abs(ny) < 0.75f)
                            pixel = fill;
                        if (Mathf.Abs(nx) + Mathf.Abs(ny) < 0.35f)
                            pixel = inner;
                        // Small center eye
                        if (Mathf.Abs(nx) < 0.12f && Mathf.Abs(ny) < 0.12f)
                            pixel = Color.white;
                        break;

                    case EnemyType.Fast:
                        // Triangle pointing left with speed lines
                        if (ny < 0.85f * (1f - nx) && ny > -0.85f * (1f - nx) && nx > -0.7f)
                            pixel = fill;
                        // Inner triangle
                        if (ny < 0.5f * (1f - nx) && ny > -0.5f * (1f - nx) && nx > -0.3f)
                            pixel = inner;
                        // Speed line
                        if (nx < -0.5f && Mathf.Abs(ny) < 0.06f)
                            pixel = fill;
                        break;

                    case EnemyType.Tank:
                        // Hexagon - 6 sided polygon
                        float dist = Mathf.Sqrt(nx * nx + ny * ny);
                        if (dist < 0.78f)
                        {
                            // Hexagon boundary check
                            float angle = Mathf.Atan2(ny, nx);
                            float hexR = 0.78f / Mathf.Max(Mathf.Abs(Mathf.Cos(angle)), Mathf.Abs(Mathf.Sin(angle)),
                                                            Mathf.Abs(Mathf.Cos(angle - Mathf.PI / 3f)),
                                                            Mathf.Abs(Mathf.Cos(angle + Mathf.PI / 3f)));
                            if (dist < hexR)
                                pixel = fill;
                        }
                        // Inner circle
                        if (dist < 0.35f)
                            pixel = inner;
                        // Center dot
                        if (dist < 0.1f)
                            pixel = Color.white;
                        break;
                }

                pixels[y * size + x] = pixel;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    public enum EnemyType { Basic, Fast, Tank }

    public static Texture2D CreateBossTexture()
    {
        int size = 96;
        Texture2D tex = new Texture2D(size, size);
        Color clear = Color.clear;
        Color fill = Color.white;
        Color inner = new Color(0.9f, 0.9f, 0.9f);
        Color core = new Color(1f, 0.8f, 0.2f);
        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float nx = (x / (float)size) * 2f - 1f;
                float ny = (y / (float)size) * 2f - 1f;
                float dist = Mathf.Sqrt(nx * nx + ny * ny);

                Color pixel = clear;

                // Outer crown — 8-pointed star shape
                float angle = Mathf.Atan2(ny, nx);
                // Star radius varies with angle
                float starR = 0.85f + 0.15f * Mathf.Sin(angle * 4f);
                if (dist < starR)
                {
                    // Arm highlights — cross pattern
                    float armFactor = Mathf.Abs(Mathf.Sin(angle * 2f));
                    if (dist < 0.5f)
                        pixel = core; // Golden core
                    else if (dist < starR * 0.7f)
                        pixel = armFactor > 0.7f ? fill : inner;
                    else
                        pixel = fill;

                    // Inner ring detail
                    if (dist > 0.25f && dist < 0.35f)
                        pixel = Color.Lerp(pixel, new Color(0.5f, 0.5f, 0.5f), 0.5f);

                    // Sharp edge glow
                    if (dist > starR * 0.88f)
                        pixel = Color.Lerp(pixel, clear, (dist - starR * 0.88f) / (starR * 0.12f));
                }

                pixels[y * size + x] = pixel;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    public static Texture2D CreateProjectileTexture()
    {
        int size = 16;
        Texture2D tex = new Texture2D(size, size);
        Color clear = Color.clear;
        Color fill = Color.white;
        Color bright = new Color(1f, 1f, 1f); // core highlight
        Color[] pixels = new Color[size * size];

        int r = size / 2;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float nx = (x - r) / (float)r;
                float ny = (y - r) / (float)r;

                // Elongated diamond (pointing right = x positive)
                float widthAtY = 1f - Mathf.Abs(ny) * 0.6f;
                bool inside = nx >= -widthAtY * 0.3f && nx <= widthAtY && Mathf.Abs(ny) <= 1f;

                // Sharper tip on the right side
                if (inside && nx > widthAtY * 0.6f)
                    inside = Mathf.Abs(ny) < 1f - (nx - widthAtY * 0.6f) / (widthAtY * 0.4f) * 0.8f;

                if (inside)
                {
                    // Bright core near center
                    float coreDist = Mathf.Sqrt(nx * nx + ny * ny);
                    pixels[y * size + x] = coreDist < 0.3f ? bright : fill;
                }
                else
                {
                    pixels[y * size + x] = clear;
                }
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    public static Texture2D CreateXPOrbTexture()
    {
        int size = 24;
        Texture2D tex = new Texture2D(size, size);
        Color clear = Color.clear;
        Color fill = Color.white;
        Color[] pixels = new Color[size * size];

        int r = size / 2;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int dx = x - r;
                int dy = y - r;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = Mathf.Clamp01(1f - dist / r);
                pixels[y * size + x] = new Color(1, 1, 1, alpha);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    public static Texture2D CreateWallTexture()
    {
        int size = 32;
        Texture2D tex = new Texture2D(size, size);
        Color fill = new Color(0.4f, 0.15f, 0.6f);
        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool border = x < 3 || x >= size - 3 || y < 3 || y >= size - 3;
                bool grid = (x / 6 + y / 6) % 2 == 0;
                pixels[y * size + x] = border ? fill : (grid ? fill * 0.8f : fill * 0.6f);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }
}
