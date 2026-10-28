using UnityEngine;

/// <summary>Slow and tough. Takes a percentage less damage from every hit.</summary>
public class TankEnemy : Enemy
{
    [Header("Tank")]
    public float speedMultiplier = 0.5f;
    [Range(0f, 0.9f)] public float damageReduction = 0.3f;

    protected override void Reset()
    {
        base.Reset();
        maxHealth = 400f;
        resourceReward = 60;
        applyLook = true;
        bodyColor = new Color(0.4f, 0.4f, 0.45f);   // dark grey
        sizeMultiplier = 1.4f;
    }

    protected override void Start()
    {
        base.Start();
        if (movement != null)
            movement.moveSpeed *= speedMultiplier;
    }

    public override void TakeDamage(float damage)
    {
        base.TakeDamage(damage * (1f - damageReduction));
    }
}
