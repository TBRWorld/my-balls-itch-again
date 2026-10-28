using System;
using UnityEngine;

/// <summary>
/// The boss. Inherits from TankEnemy (so Boss -> Tank -> Enemy): slow, tough and
/// immune to stun. Below the phase threshold it enrages and starts summoning minions.
/// </summary>
public class BossEnemy : TankEnemy
{
    /// <summary>Raised once when the boss dies. Hook a win screen up to this.</summary>
    public static event Action BossDefeated;

    [Header("Boss")]
    [Range(0.1f, 0.9f)] public float phaseTwoAtHealth = 0.5f;
    public float enrageSpeedMultiplier = 2f;

    [Header("Minions (phase 2)")]
    public GameObject minionPrefab;
    public int minionsPerWave = 2;
    public float minionInterval = 6f;

    private bool phaseTwo;
    private float nextMinionTime;

    public override bool CanBeStunned => false;

    protected override void Reset()
    {
        base.Reset();
        maxHealth = 1500f;
        resourceReward = 500;
        damageReduction = 0.25f;
        speedMultiplier = 0.6f;
        bodyColor = new Color(0.8f, 0.1f, 0.1f);   // red
        sizeMultiplier = 2.2f;
    }

    public override void TakeDamage(float damage)
    {
        base.TakeDamage(damage);

        if (!IsDead && !phaseTwo && currentHealth <= maxHealth * phaseTwoAtHealth)
            EnterPhaseTwo();
    }

    private void EnterPhaseTwo()
    {
        phaseTwo = true;
        nextMinionTime = Time.time + minionInterval;

        if (movement != null)
            movement.moveSpeed *= enrageSpeedMultiplier;
    }

    private void Update()
    {
        if (!phaseTwo || IsDead || minionPrefab == null) return;
        if (Time.time < nextMinionTime) return;

        nextMinionTime = Time.time + minionInterval;

        Vector3 spawnPos = SnapToGrid(transform.position);
        for (int i = 0; i < minionsPerWave; i++)
            Instantiate(minionPrefab, spawnPos, Quaternion.identity);
    }

    protected override void OnDeath()
    {
        BossDefeated?.Invoke();
    }
}
