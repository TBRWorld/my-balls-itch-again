using UnityEngine;

public class Tower : MonoBehaviour
{
    [Header("Targeting")]
    [Tooltip("Tag used to identify enemies")]
    public string enemyTag = "Enemy";

    [Tooltip("How far the tower can detect and shoot enemies")]
    public float range = 10f;

    [Tooltip("How often the tower searches for a new target (seconds)")]
    public float targetRefreshRate = 0.2f;

    [Header("Firing")]
    [Tooltip("The bullet prefab to spawn")]
    public GameObject bulletPrefab;

    [Tooltip("Point where bullets spawn (leave empty to use tower position)")]
    public Transform firePoint;

    [Tooltip("Bullets per second")]
    public float fireRate = 1.5f;

    [Tooltip("Damage each bullet deals")]
    public float damage = 25f;

    [Header("Bullet Physics")]
    [Tooltip("Speed of the bullet in units per second")]
    public float bulletSpeed = 30f;

    [Tooltip("How long bullets live before being destroyed (seconds)")]
    public float bulletLifetime = 3f;

    [Header("Audio / FX (Optional)")]
    public AudioSource shootSound;
    public ParticleSystem muzzleFlash;

    private Transform currentTarget;
    private float fireCooldown;
    private float targetRefreshTimer;

    private void Start()
    {
        if (firePoint == null)
            firePoint = transform;

        if (bulletPrefab == null)
            Debug.LogError($"[Tower] No bullet prefab assigned on {name}!");
    }

    private void Update()
    {
        targetRefreshTimer -= Time.deltaTime;
        if (targetRefreshTimer <= 0f)
        {
            targetRefreshTimer = targetRefreshRate;
            AcquireTarget();
        }

        if (currentTarget != null)
        {
            float dist = Vector3.Distance(transform.position, currentTarget.position);
            if (dist > range || currentTarget.gameObject.activeSelf == false)
                currentTarget = null;
        }

        fireCooldown -= Time.deltaTime;
        if (currentTarget != null && fireCooldown <= 0f)
        {
            Fire();
            fireCooldown = 1f / Mathf.Max(fireRate, 0.0001f);
        }
    }

    private void AcquireTarget()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag(enemyTag);
        Transform nearest = null;
        float nearestSqr = range * range;
        Vector3 myPos = transform.position;

        foreach (var enemy in enemies)
        {
            if (enemy == null || !enemy.activeSelf) continue;
            Vector3 diff = enemy.transform.position - myPos;
            diff.y = 0f;
            float sqr = diff.sqrMagnitude;
            if (sqr < nearestSqr)
            {
                nearestSqr = sqr;
                nearest = enemy.transform;
            }
        }

        currentTarget = nearest;
    }

    private void Fire()
    {
        if (bulletPrefab == null || currentTarget == null) return;

        Vector3 origin = firePoint.position;
        Vector3 toTarget = currentTarget.position - origin;
        Vector3 finalDir = toTarget.normalized;

        GameObject bulletObj = Instantiate(bulletPrefab, origin, Quaternion.LookRotation(finalDir));

        Bullet bullet = bulletObj.GetComponent<Bullet>();
        if (bullet != null)
        {
            bullet.Setup(finalDir, bulletSpeed, bulletLifetime, damage);
        }
        else
        {
            Rigidbody rb = bulletObj.GetComponent<Rigidbody>();
            if (rb == null) rb = bulletObj.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.linearVelocity = finalDir * bulletSpeed;
            Destroy(bulletObj, bulletLifetime);
        }

        if (shootSound != null) shootSound.Play();
        if (muzzleFlash != null) muzzleFlash.Play();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, range);
    }
}
