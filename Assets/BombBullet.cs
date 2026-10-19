using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A bullet that explodes on impact, damaging every enemy inside a radius.
/// Lives on the BulletBomb prefab.
/// </summary>
public class BombBullet : Bullet
{
    [Header("Explosion")]
    [Tooltip("Radius of the explosion in world units")]
    public float explosionRadius = 2.5f;

    [Tooltip("Damage dealt to every enemy caught in the explosion")]
    public float areaDamage = 40f;

    protected override void HandleHit(GameObject other, Vector3 hitPoint)
    {
        Collider[] hits = Physics.OverlapSphere(hitPoint, explosionRadius);
        HashSet<GameObject> damaged = new HashSet<GameObject>();

        foreach (Collider col in hits)
        {
            if (col == null || !col.CompareTag(enemyTag)) continue;
            if (!damaged.Add(col.gameObject)) continue;

            col.SendMessage("TakeDamage", areaDamage, SendMessageOptions.DontRequireReceiver);
        }

        if (hitEffectPrefab != null)
            Instantiate(hitEffectPrefab, hitPoint, Quaternion.identity);

        if (destroyOnHit)
            Destroy(gameObject);
    }
}
