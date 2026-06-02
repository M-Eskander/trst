using UnityEngine;

public class TankEnemy : EnemyBase
{
    [Header("Tank Enemy")]
    public float chargeRange = 6f;
    public float chargeSpeed = 8f;
    public float chargeCooldown = 3f;
    public float chargeWindup = 0.5f;
    public GameObject shieldVisual;

    private enum TankState { Approaching, Windup, Charging, Cooldown }
    private TankState state = TankState.Approaching;
    private float stateTimer;
    private Vector2 chargeDirection;

    protected override void Start()
    {
        maxHealth = 80;
        moveSpeed = 1.5f;
        contactDamage = 25;
        scoreValue = 30;
        xpDropAmount = 20f;
        enemyColor = new Color(0.9f, 0.4f, 0.1f);

        base.Start();

        stateTimer = 0f;
    }

    protected override void FixedUpdate()
    {
        if (player == null || GameManager.Instance.IsGameOver)
        {
            if (rb != null) rb.linearVelocity = Vector2.zero;
            return;
        }

        float dist = Vector2.Distance(transform.position, player.position);
        Vector2 toPlayer = ((Vector2)player.position - (Vector2)transform.position).normalized;

        switch (state)
        {
            case TankState.Approaching:
                rb.linearVelocity = toPlayer * moveSpeed * slowFactor;

                if (dist < chargeRange)
                {
                    state = TankState.Windup;
                    stateTimer = chargeWindup;
                }

                float approachAngle = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0, 0, approachAngle);
                break;

            case TankState.Windup:
                rb.linearVelocity = Vector2.zero;
                stateTimer -= Time.fixedDeltaTime;

                if (spriteRenderer != null)
                {
                    float pulse = Mathf.PingPong(Time.time * 10f, 1f);
                    spriteRenderer.color = Color.Lerp(enemyColor, Color.red, pulse);
                }

                if (stateTimer <= 0f)
                {
                    chargeDirection = ((Vector2)player.position - (Vector2)transform.position).normalized;
                    state = TankState.Charging;
                    stateTimer = 0.5f;
                }
                break;

            case TankState.Charging:
                rb.linearVelocity = chargeDirection * chargeSpeed;

                float chargeAngle = Mathf.Atan2(chargeDirection.y, chargeDirection.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0, 0, chargeAngle);

                stateTimer -= Time.fixedDeltaTime;
                if (stateTimer <= 0f || dist > chargeRange * 1.5f)
                {
                    state = TankState.Cooldown;
                    stateTimer = 1f;
                }
                break;

            case TankState.Cooldown:
                rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, 5f * Time.fixedDeltaTime);
                stateTimer -= Time.fixedDeltaTime;

                if (stateTimer <= 0f)
                {
                    state = TankState.Approaching;
                }
                break;
        }
    }

    protected override void Die()
    {
        // Extra explosion for tanks
        VFXManager.Instance?.SpawnExplosion(transform.position, Color.yellow, 1.5f);
        CameraFollow.Instance?.Shake(0.7f);
        base.Die();
    }
}
