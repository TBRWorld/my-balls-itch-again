using UnityEngine;

/// <summary>Fast and fragile. Hard to hit, easy to kill.</summary>
public class RunnerEnemy : Enemy
{
    [Header("Runner")]
    public float speedMultiplier = 2.5f;

    protected override void Reset()
    {
        base.Reset();
        maxHealth = 40f;
        resourceReward = 15;
        applyLook = true;
        bodyColor = new Color(1f, 0.9f, 0.2f);   // yellow
        sizeMultiplier = 0.7f;
    }

    protected override void Start()
    {
        base.Start();
        if (movement != null)
            movement.moveSpeed *= speedMultiplier;
    }
}
