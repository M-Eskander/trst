using UnityEngine;

public class FastEnemy : EnemyBase
{
    [Header("Fast Enemy")]
    public float strafeDistance = 3f;
    public float strafeSpeed = 1f;
    public float burstCooldown = 2f;

    private Vector2 strafeDir;
    private float strafeTimer;
    private float burstTimer;

    protected override void Start()
    {
        maxHealth = 15;
        moveSpeed = 4.5f;
        contactDamage = 8;
        scoreValue = 15;
        xpDropAmount = 8f;
        enemyColor = new Color(0.8f, 0.2f, 0.8f);

        base.Start();

        strafeDir = Random.insideUnitCircle.normalized;
        strafeTimer = Random.Range(0.5f, 1.5f);
        burstTimer = burstCooldown;
    }

    protected override void FixedUpdate()
    {
        if (player == null || GameManager.Instance.IsGameOver)
        {
            if (rb != null) rb.linearVelocity = Vector2.zero;
            return;
        }

        strafeTimer -= Time.fixedDeltaTime;
        burstTimer -= Time.fixedDeltaTime;

        if (strafeTimer <= 0f)
        {
            strafeDir = Random.insideUnitCircle.normalized;
            strafeTimer = Random.Range(0.3f, 1f);
        }

        Vector2 toPlayer = ((Vector2)player.position - (Vector2)transform.position).normalized;
        float dist = Vector2.Distance(transform.position, player.position);
        Vector2 moveDir;

        if (dist < strafeDistance)
        {
            // Circle around player
            Vector2 tangent = Vector2.Perpendicular(toPlayer);
            moveDir = (toPlayer * 0.3f + tangent * 0.7f).normalized;
        }
        else
        {
            // Approach with strafe
            moveDir = (toPlayer + strafeDir * 0.3f).normalized;
        }

        float speedMultiplier = burstTimer < 0.3f ? 1.8f : 1f;
        if (burstTimer <= 0f)
        {
            burstTimer = burstCooldown;
        }

        rb.linearVelocity = moveDir * moveSpeed * speedMultiplier * slowFactor;

        float angle = Mathf.Atan2(moveDir.y, moveDir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }
}
