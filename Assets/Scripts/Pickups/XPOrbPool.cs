using UnityEngine;
using System.Collections.Generic;

public class XPOrbPool : MonoBehaviour
{
    public static XPOrbPool Instance { get; private set; }

    [Header("XP Orb Settings")]
    public GameObject xpOrbPrefab;
    public int poolSize = 50;
    public float attractRadius = 3f;
    public float attractSpeed = 8f;

    private Queue<GameObject> orbPool = new Queue<GameObject>();
    private List<GameObject> activeOrbs = new List<GameObject>();

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // Pre-instantiate pool
        for (int i = 0; i < poolSize; i++)
        {
            GameObject orb = Instantiate(xpOrbPrefab, Vector3.zero, Quaternion.identity, transform);
            orb.SetActive(false);
            orbPool.Enqueue(orb);
        }
    }

    void Update()
    {
        if (PlayerController.Instance == null) return;

        Vector2 playerPos = PlayerController.Instance.transform.position;

        // Attract nearby orbs toward player
        for (int i = activeOrbs.Count - 1; i >= 0; i--)
        {
            if (activeOrbs[i] == null || !activeOrbs[i].activeInHierarchy)
            {
                activeOrbs.RemoveAt(i);
                continue;
            }

            GameObject orb = activeOrbs[i];
            float dist = Vector2.Distance(orb.transform.position, playerPos);

            if (dist < attractRadius)
            {
                Vector2 newPos = Vector2.MoveTowards(
                    orb.transform.position, playerPos, attractSpeed * Time.deltaTime);
                orb.transform.position = new Vector3(newPos.x, newPos.y, -1f);
            }

            // Collected
            if (dist < 0.3f)
            {
                CollectOrb(orb);
            }
        }
    }

    public void SpawnOrb(Vector2 position, float xpAmount)
    {
        if (orbPool.Count == 0) return;

        GameObject orb = orbPool.Dequeue();
        orb.transform.position = new Vector3(position.x, position.y, -1f);
        orb.SetActive(true);
        orb.transform.localScale = Vector3.one * Mathf.Clamp(0.15f + xpAmount * 0.01f, 0.15f, 0.6f);

        var orbScript = orb.GetComponent<XPOrb>();
        if (orbScript != null) orbScript.SetXPAmount(xpAmount);

        activeOrbs.Add(orb);
    }

    void CollectOrb(GameObject orb)
    {
        XPOrb orbScript = orb.GetComponent<XPOrb>();
        if (orbScript != null)
        {
            float amount = orbScript.GetXPAmount();
            UpgradeManager.Instance?.AddXP(amount);
        }

        orb.SetActive(false);
        activeOrbs.Remove(orb);
        orbPool.Enqueue(orb);

        VFXManager.Instance?.SpawnXPPickup(orb.transform.position);
        AudioManager.Instance?.PlayHitmarker();
    }
}
