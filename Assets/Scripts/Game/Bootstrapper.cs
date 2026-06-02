using UnityEngine;

/// <summary>
/// Auto-creates the Bootstrap after the scene has loaded,
/// so Camera.main and other scene-object references exist.
/// Just hit Play — no scene setup needed.
/// </summary>
public static class Bootstrapper
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        if (Object.FindFirstObjectByType<Bootstrap>() != null)
            return;

        new GameObject("Bootstrap", typeof(Bootstrap));
    }
}
