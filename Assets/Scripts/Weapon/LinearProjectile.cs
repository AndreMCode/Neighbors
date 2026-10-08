using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class LinearProjectile : Projectile
{
    [SerializeField] private LayerMask hitLayers;

    private Rigidbody2D rb;
    private Vector2 startPosition;
    private Vector2 direction;
    private bool hasHit = false;

    private void OnEnable()
    {
        // Reset single-hit flag when retrieved from ProjectilePoolManager
        hasHit = false;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public override void Initialize(WeaponData data, Vector2 fireDirection)
    {
        weaponData = data;
        hasHit = false;
        direction = fireDirection.normalized;
        startPosition = transform.position;

        // Orient projectile sprite to point along movement direction
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    private void FixedUpdate()
    {
        if (weaponData == null) return;

        // Move projectile straight forward
        rb.linearVelocity = direction * weaponData.ProjectileSpeed;

        // Range check: Recycle if traveled past MaxRange
        if (((Vector2)transform.position - startPosition).sqrMagnitude >= weaponData.MaxRange * weaponData.MaxRange)
        {
            ReleaseToPool();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Guard against multi-hit evaluation on overlapping colliders in a single frame
        if (hasHit) return;

        // Ignore trigger volumes or layers not in hitLayers
        if (((1 << collision.gameObject.layer) & hitLayers) == 0) return;

        // Search up the hierarchy to find IDamageable on the parent or the object itself
        IDamageable target = collision.GetComponentInParent<IDamageable>();
        if (target != null)
        {
            target.TakeDamage(weaponData.Damage, direction, weaponData.KnockbackForce);
            hasHit = true;
        }

        // Return projectile to pool on impact
        ReleaseToPool();
    }
}