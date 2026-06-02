using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public static CameraFollow Instance { get; private set; }

    public Transform target;
    public float smoothSpeed = 8f;
    public Vector3 offset = new Vector3(0, 0, -10);
    public float orthographicSize = 8f;
    public float shakeDecay = 2f;

    private Camera cam;
    private Vector3 shakeOffset;
    private float currentShake;
    private bool foundTarget;

    void Awake()
    {
        Instance = this;
        cam = GetComponent<Camera>();
    }

    void Start()
    {
        if (cam != null) cam.orthographicSize = orthographicSize;
        // Auto-find player after a brief delay
        Invoke(nameof(FindPlayer), 0.1f);
    }

    void FindPlayer()
    {
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) player = GameObject.Find("Player");
            if (player != null) target = player.transform;
        }
        foundTarget = true;
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPos = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desiredPos, smoothSpeed * Time.deltaTime);

        // Screen shake
        if (currentShake > 0.01f)
        {
            shakeOffset = Random.insideUnitSphere * currentShake;
            shakeOffset.z = 0;
            transform.position += shakeOffset;
            currentShake = Mathf.Lerp(currentShake, 0f, shakeDecay * Time.deltaTime);
        }
    }

    public void Shake(float intensity = 0.5f)
    {
        currentShake = Mathf.Max(currentShake, intensity);
    }
}
