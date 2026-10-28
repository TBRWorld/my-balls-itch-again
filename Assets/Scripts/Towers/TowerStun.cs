using UnityEngine;

/// <summary>
/// Stun / freeze tower. Inherits all targeting and firing from <see cref="Tower"/>.
/// The actual freeze is carried by the StunBullet assigned as the bulletPrefab,
/// so this class only forwards its stun settings to that bullet.
/// </summary>
public class TowerStun : Tower
{
    [Header("Stun")]
    [Tooltip("How long enemies stay frozen / slowed (seconds)")]
    public float stunDuration = 2f;

    [Tooltip("Movement multiplier while stunned. 0 = full freeze, 0.5 = half speed")]
    [Range(0f, 1f)]
    public float slowMultiplier = 0f;

    protected override void ConfigureBullet(Bullet bullet, Vector3 direction)
    {
        if (bullet is StunBullet stunBullet)
        {
            stunBullet.stunDuration = stunDuration;
            stunBullet.slowMultiplier = slowMultiplier;
        }
    }
}
