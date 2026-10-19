using UnityEngine;

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

    private void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.AddResources(resourceReward);
        }

        if (deathEffectPrefab != null)
            Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);

        if (deathSound != null)
        {
            AudioSource.PlayClipAtPoint(deathSound.clip, transform.position);
        }

        Destroy(gameObject);
    }
}
