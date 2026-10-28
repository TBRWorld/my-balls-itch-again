using UnityEngine;

/// <summary>
/// A bullet that freezes / slows the enemy it hits, on top of dealing damage.
/// Lives on the BulletStun prefab.
/// </summary>
public class StunBullet : Bullet
{
    [Header("Stun")]
    [Tooltip("How long the target stays frozen / slowed (seconds)")]
    public float stunDuration = 2f;

    [Tooltip("Movement multiplier while stunned. 0 = full freeze, 0.5 = half speed")]
    [Range(0f, 1f)]
    public float slowMultiplier = 0f;

    protected override void HandleHit(GameObject other, Vector3 hitPoint)
    {
        if (hasHit) return;

        Enemy enemy = other.GetComponentInParent<Enemy>();
        if (enemy != null)
        {
            // Bosses and other immune enemies ignore stun.
            if (enemy.CanBeStunned)
                ApplyStun(enemy.gameObject);
        }
        else if (other.CompareTag(enemyTag))
        {
            ApplyStun(other);
        }

        base.HandleHit(other, hitPoint);
    }

    private void ApplyStun(GameObject enemy)
    {
        StunEffect effect = enemy.GetComponent<StunEffect>();
        if (effect == null)
            effect = enemy.AddComponent<StunEffect>();

        effect.ApplyStun(stunDuration, slowMultiplier);
    }
}
