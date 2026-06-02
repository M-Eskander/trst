using UnityEngine;

public class XPOrb : MonoBehaviour
{
    private float xpAmount = 5f;
    private SpriteRenderer spriteRenderer;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void OnEnable()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.Lerp(new Color(1f, 0.9f, 0.2f), new Color(0.2f, 1f, 1f), Random.value);
            spriteRenderer.color = new Color(spriteRenderer.color.r, spriteRenderer.color.g, spriteRenderer.color.b, 0.9f);
        }
    }

    public void SetXPAmount(float amount)
    {
        xpAmount = amount;
    }

    public float GetXPAmount() => xpAmount;

    void Update()
    {
        // Subtle bob
        transform.position += new Vector3(0, Mathf.Sin(Time.time * 3f + GetInstanceID()) * 0.003f, 0);
    }
}
