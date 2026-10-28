using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Placeable trap. When an enemy walks onto it, it deals a burst of damage to
/// every enemy in range and is then consumed (destroys itself).
///
/// It detects enemies by proximity each frame, so it needs no collider or
/// Rigidbody and works regardless of the enemy's physics setup.
/// </summary>
public class Trap : MonoBehaviour
{
    [Header("Trigger")]
    [Tooltip("Tag used to identify enemies")]
    public string enemyTag = "Enemy";

    [Tooltip("Radius in world units that counts as 'walking over' the trap")]
    public float triggerRadius = 2f;

    [Header("Economy")]
    public int cost = 50;

    [Header("Effect")]
    [Tooltip("Damage dealt to every enemy caught by the trap")]
    public float damage = 150f;

    [Header("FX (Optional)")]
    public GameObject triggerEffectPrefab;
    public AudioClip triggerSound;

    private bool triggered;
    private readonly Collider[] overlapBuffer = new Collider[16];

    private void Update()
    {
        if (triggered) return;

        int count = Physics.OverlapSphereNonAlloc(transform.position, triggerRadius, overlapBuffer);
        for (int i = 0; i < count; i++)
        {
            Collider col = overlapBuffer[i];
            if (col != null && col.CompareTag(enemyTag))
            {
                Trigger();
                return;
            }
        }
    }

    private void Trigger()
    {
        triggered = true;

        int count = Physics.OverlapSphereNonAlloc(transform.position, triggerRadius, overlapBuffer);
        HashSet<GameObject> damaged = new HashSet<GameObject>();

        for (int i = 0; i < count; i++)
        {
            Collider col = overlapBuffer[i];
            if (col == null || !col.CompareTag(enemyTag)) continue;
            if (!damaged.Add(col.gameObject)) continue;

            col.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
        }

        if (triggerEffectPrefab != null)
            Instantiate(triggerEffectPrefab, transform.position, Quaternion.identity);

        if (triggerSound != null)
            AudioSource.PlayClipAtPoint(triggerSound, transform.position);

        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, triggerRadius);
    }
}
