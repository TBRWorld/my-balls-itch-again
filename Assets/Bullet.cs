using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("Damage")]
    [Tooltip("Damage dealt to enemies on hit (overridden by Tower if Setup is called)")]
    public float damage = 25f;

    [Header("Hit Detection")]
    [Tooltip("Tag of objects the bullet should damage")]
    public string enemyTag = "Enemy";

    [Tooltip("What layers the bullet can hit (-1 = everything)")]
    public LayerMask hitLayers = ~0;

    [Header("Collision")]
    [Tooltip("Should the bullet be destroyed on any hit?")]
    public bool destroyOnHit = true;

    [Tooltip("Optional particle effect spawned at impact point")]
    public GameObject hitEffectPrefab;

    [Header("Cleanup (Auto)")]
    [Tooltip("Max distance bullet can travel before self-destructing (units)")]
    public float maxTravelDistance = 100f;

    private Vector3 direction;
    private Vector3 spawnPosition;
    private float speed;
    private float lifetimeRemaining;
    private float distanceTraveled;
    private bool isSetup;

    public void Setup(Vector3 dir, float spd, float life, float dmg)
    {
        direction = dir.normalized;
        speed = spd;
        lifetimeRemaining = life;
        damage = dmg;
        isSetup = true;
        spawnPosition = transform.position;
    }

    private void Start()
    {
        spawnPosition = transform.position;

        if (!isSetup)
        {
            direction = transform.forward;
            speed = 20f;
            lifetimeRemaining = lifetimeRemaining > 0 ? lifetimeRemaining : 3f;
        }
    }

    private void Update()
    {
        Vector3 moveDir = direction;
        float moveAmount = speed * Time.deltaTime;
        transform.position += moveDir * moveAmount;
        distanceTraveled += moveAmount;

        lifetimeRemaining -= Time.deltaTime;
        if (lifetimeRemaining <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        if (distanceTraveled >= maxTravelDistance)
        {
            Destroy(gameObject);
            return;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        HandleHit(other.gameObject, other.ClosestPoint(transform.position));
    }

    private void OnCollisionEnter(Collision collision)
    {
        HandleHit(collision.gameObject, collision.GetContact(0).point);
    }

    protected virtual void HandleHit(GameObject other, Vector3 hitPoint)
    {
        if (((1 << other.layer) & hitLayers) == 0) return;

        if (other.CompareTag(enemyTag))
        {
            Debug.Log($"[Bullet] Hit enemy: {other.name} for {damage} damage");
            // TODO: When teammate's health system is merged, call damage here, e.g.:
            // other.GetComponent<Health>().TakeDamage(damage);
            other.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
        }

        if (hitEffectPrefab != null)
            Instantiate(hitEffectPrefab, hitPoint, Quaternion.identity);

        if (destroyOnHit)
            Destroy(gameObject);
    }
}
