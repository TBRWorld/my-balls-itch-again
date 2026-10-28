using UnityEngine;

/// <summary>Has a shield that soaks damage first and regenerates if it is left alone.</summary>
public class ShieldedEnemy : Enemy
{
    [Header("Shield")]
    public float maxShield = 60f;
    public float currentShield;
    [Tooltip("Seconds without taking damage before the shield starts to recharge")]
    public float regenDelay = 4f;
    public float regenPerSecond = 20f;

    private float lastHitTime;

    protected override void Reset()
    {
        base.Reset();
        maxHealth = 80f;
        resourceReward = 35;
        applyLook = true;
        bodyColor = new Color(0.3f, 0.7f, 1f);   // light blue
    }

    protected override void Start()
    {
        base.Start();
        currentShield = maxShield;
    }

    private void Update()
    {
        if (IsDead || currentShield >= maxShield) return;
        if (Time.time - lastHitTime < regenDelay) return;

        currentShield = Mathf.Min(maxShield, currentShield + regenPerSecond * Time.deltaTime);
    }

    public override void TakeDamage(float damage)
    {
        lastHitTime = Time.time;

        float absorbed = Mathf.Min(currentShield, damage);
        currentShield -= absorbed;
        damage -= absorbed;

        if (damage > 0f)
            base.TakeDamage(damage);
    }
}
