using UnityEngine;

/// <summary>
/// Bomb / AOE tower. Inherits all targeting and firing from <see cref="Tower"/>.
/// The splash damage is carried by the BombBullet assigned as the bulletPrefab,
/// so this class only forwards its explosion settings to that bullet.
/// </summary>
public class TowerBomb : Tower
{
    [Header("Explosion")]
    [Tooltip("Radius of the explosion in world units")]
    public float explosionRadius = 2.5f;

    [Tooltip("Damage dealt to every enemy caught in the explosion")]
    public float areaDamage = 40f;

    protected override void ConfigureBullet(Bullet bullet, Vector3 direction)
    {
        if (bullet is BombBullet bombBullet)
        {
            bombBullet.explosionRadius = explosionRadius;
            bombBullet.areaDamage = areaDamage;
        }
    }
}
