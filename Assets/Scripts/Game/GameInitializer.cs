using UnityEngine;

/// <summary>
/// Sets up the game scene at runtime: creates procedural assets,
/// builds the arena, and wires up any missing references.
/// </summary>
public class GameInitializer : MonoBehaviour
{
    [Header("Arena Settings")]
    public float arenaWidth = 28f;
    public float arenaHeight = 20f;
    public Color backgroundColor = new Color(0.02f, 0.01f, 0.05f);

    void Awake()
    {
        SetupCamera();
        BuildArena();
    }

    void SetupCamera()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        cam.backgroundColor = backgroundColor;
        cam.clearFlags = CameraClearFlags.SolidColor;
    }

    void BuildArena()
    {
        CreateBackgroundGrid();
    }

    void CreateBackgroundGrid()
    {
        GameObject grid = new GameObject("BackgroundGrid");
        var gridSr = grid.AddComponent<SpriteRenderer>();
        gridSr.sortingOrder = -10;

        int gridTexSize = 512;
        Texture2D gridTex = new Texture2D(gridTexSize, gridTexSize);
        Color[] pixels = new Color[gridTexSize * gridTexSize];
        Color gridColor = new Color(0.05f, 0.02f, 0.1f);
        Color bgColor = new Color(0.02f, 0.01f, 0.05f);

        for (int y = 0; y < gridTexSize; y++)
        {
            for (int x = 0; x < gridTexSize; x++)
            {
                bool isLine = (x % 32 == 0 || y % 32 == 0);
                pixels[y * gridTexSize + x] = isLine ? gridColor : bgColor;
            }
        }

        gridTex.SetPixels(pixels);
        gridTex.Apply();

        gridSr.sprite = Sprite.Create(gridTex, new Rect(0, 0, gridTexSize, gridTexSize), Vector2.one * 0.5f, 32);
        gridSr.color = Color.white;
        grid.transform.localScale = new Vector3(arenaWidth * 2, arenaHeight * 2, 1);
        grid.transform.position = Vector3.zero;
    }
}
