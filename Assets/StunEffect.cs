using UnityEngine;

/// <summary>
/// Added to an enemy at runtime when it is hit by a stun bullet. Temporarily
/// reduces the enemy's movement speed and restores it when the stun expires.
/// Works against the existing EnemyMovement, so no change to the enemy scripts
/// is required.
/// </summary>
public class StunEffect : MonoBehaviour
{
    private EnemyMovement movement;
    private float originalSpeed;
    private float remaining;
    private bool hasOriginalSpeed;

    public bool IsStunned => remaining > 0f;

    public void ApplyStun(float duration, float speedMultiplier)
    {
        if (movement == null)
            movement = GetComponent<EnemyMovement>();

        if (movement == null)
            return;

        // Capture the enemy's real speed only once, before the first stun.
        if (!hasOriginalSpeed)
        {
            originalSpeed = movement.moveSpeed;
            hasOriginalSpeed = true;
        }

        movement.moveSpeed = originalSpeed * Mathf.Clamp01(speedMultiplier);
        remaining = Mathf.Max(remaining, duration);
    }

    private void Update()
    {
        if (remaining <= 0f) return;

        remaining -= Time.deltaTime;
        if (remaining <= 0f && movement != null)
            movement.moveSpeed = originalSpeed;
    }

    private void OnDisable()
    {
        if (movement != null && hasOriginalSpeed)
            movement.moveSpeed = originalSpeed;
    }
}
