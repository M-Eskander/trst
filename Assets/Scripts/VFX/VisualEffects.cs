using UnityEngine;

/// <summary>
/// Full-screen visual effects manager.
/// Delegates to UIManager for proper UI-based effects.
/// </summary>
public class VisualEffects : MonoBehaviour
{
    public static VisualEffects Instance { get; private set; }

    void Awake()
    {
        Instance = this;
    }

    public void DamageFlash()
    {
        UIManager.Instance?.DamageFlash();
    }

    public void LevelUpFlash()
    {
        UIManager.Instance?.FlashScreen(new Color(1f, 0.8f, 0.2f, 0.3f), 0.5f);
    }

    public void BossFlash()
    {
        UIManager.Instance?.FlashScreen(new Color(0.5f, 0f, 0.2f, 0.4f), 0.6f);
    }
}
