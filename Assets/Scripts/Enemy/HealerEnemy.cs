using System.Collections.Generic;
using UnityEngine;

/// <summary>Heals other enemies around it on a timer. Kill it first.</summary>
public class HealerEnemy : Enemy
{
    [Header("Healer")]
    public float healRadius = 8f;
    public float healAmount = 15f;
    public float healInterval = 2f;

    private float nextHealTime;

    protected override void Reset()
    {
        base.Reset();
        maxHealth = 70f;
        resourceReward = 40;
        applyLook = true;
        bodyColor = new Color(0.3f, 1f, 0.4f);   // green
    }

    protected override void Start()
    {
        base.Start();
        nextHealTime = Time.time + healInterval;
    }

    private void Update()
    {
        if (Time.time < nextHealTime) return;
        nextHealTime = Time.time + healInterval;

        HashSet<Enemy> healed = new HashSet<Enemy>();
        foreach (Collider col in Physics.OverlapSphere(transform.position, healRadius))
        {
            Enemy other = col.GetComponentInParent<Enemy>();
            if (other == null || other == this || !healed.Add(other)) continue;
            other.Heal(healAmount);
        }
    }
}
