using UnityEngine;

/// <summary>
/// Base class for every enemy. Subclasses (Runner, Tank, Healer, Splitter,
/// Shielded, Boss) override the virtual members to change behaviour.
/// Bullets, bombs and traps still call TakeDamage via SendMessage, so
/// overriding TakeDamage in a subclass works without touching them.
/// </summary>
public class Enemy : MonoBehaviour
{
    [Header("Health")]
    public float maxHealth = 100f;
    public float currentHealth;

    [Header("Death FX (Optional)")]
    public GameObject deathEffectPrefab;
    public AudioSource deathSound;

    [Header("Rewards")]
    public int resourceReward = 25;

    [Header("Look (Optional)")]
    [Tooltip("Apply the colour and size below when the enemy spawns")]
    public bool applyLook = false;
    public Color bodyColor = Color.white;
    public float sizeMultiplier = 1f;

    protected EnemyMovement movement;
    protected WorldGen worldGen;
    private bool isDead;

    public bool IsDead => isDead;

    /// <summary>Stun bullets check this. Override to make an enemy immune.</summary>
    public virtual bool CanBeStunned => true;

    // Called by Unity in the editor when the component is added.
    // Subclasses override it to give themselves default stats.
    protected virtual void Reset() { }

    protected virtual void Start()
    {
        currentHealth = maxHealth;
        movement = GetComponent<EnemyMovement>();

        if (applyLook)
        {
            transform.localScale *= sizeMultiplier;
            Renderer r = GetComponentInChildren<Renderer>();
            if (r != null) r.material.color = bodyColor;
        }
    }

    public virtual void TakeDamage(float damage)
    {
        if (isDead) return;

        currentHealth -= damage;

        if (currentHealth <= 0f)
            Die();
    }

    public virtual void Heal(float amount)
    {
        if (isDead) return;
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
    }

    /// <summary>Hook for subclasses. Runs once, right before the enemy is destroyed.</summary>
    protected virtual void OnDeath() { }

    protected void Die()
    {
        if (isDead) return;   // two bullets in one frame must not pay out twice
        isDead = true;

        if (ResourceManager.Instance != null)
            ResourceManager.Instance.AddResources(resourceReward);

        if (deathEffectPrefab != null)
            Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);

        if (deathSound != null)
            AudioSource.PlayClipAtPoint(deathSound.clip, transform.position);

        OnDeath();
        Destroy(gameObject);
    }

    /// <summary>Enemies move cell to cell, so anything spawned mid-move must be put on the grid.</summary>
    protected Vector3 SnapToGrid(Vector3 position)
    {
        if (worldGen == null) worldGen = FindFirstObjectByType<WorldGen>();
        if (worldGen == null) return position;

        float s = worldGen.spacing;
        return new Vector3(
            Mathf.Round(position.x / s) * s,
            Mathf.Round(position.y / s) * s,
            Mathf.Round(position.z / s) * s);
    }
}
