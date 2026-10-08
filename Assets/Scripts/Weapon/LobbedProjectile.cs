using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class LobbedProjectile : Projectile
{
    [Header("Visual Arc Reference")]
    [SerializeField] private Transform visualSpriteTransform;
    [SerializeField] private LayerMask aoeHitLayers;

    private Vector2 startPosition;
    private Vector2 targetPosition;
    private float elapsedTime;

    public override void Initialize(WeaponData data, Vector2 fireDirection)
    {
        weaponData = data;
        startPosition = transform.position;
        elapsedTime = 0f;

        // Calculate ground landing target point based on MaxRange
        targetPosition = startPosition + (fireDirection.normalized * weaponData.MaxRange);

        if (visualSpriteTransform != null)
        {
            visualSpriteTransform.localPosition = Vector3.zero;
        }
    }

    private void Update()
    {
        if (weaponData == null) return;

        elapsedTime += Time.deltaTime;
        float t = Mathf.Clamp01(elapsedTime / weaponData.LobDuration);

        // Move root position along the ground plane toward target position
        transform.position = Vector2.Lerp(startPosition, targetPosition, t);

        // Parabolic Y-axis offset on visual child: 4 * PeakHeight * t * (1 - t)
        if (visualSpriteTransform != null)
        {
            float heightOffset = 4f * weaponData.ArcPeakHeight * t * (1f - t);
            visualSpriteTransform.localPosition = new Vector3(0f, heightOffset, 0f);
        }

        // Trigger AoE detonation on landing
        if (t >= 1.0f)
        {
            DetonateAoE();

            if (weaponData.ImpactVFXPrefab != null && VFXPoolManager.Instance != null)
            {
                VFXPoolManager.Instance.SpawnVFX(weaponData.ImpactVFXPrefab, transform.position, Quaternion.identity);
            }

            ReleaseToPool();
        }
    }

    private void DetonateAoE()
    {
        // Detect all entities within AoE explosion radius
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, weaponData.AoeRadius, aoeHitLayers);

        foreach (Collider2D hit in hits)
        {
            IDamageable target = hit.GetComponentInParent<IDamageable>();
            if (target != null)
            {
                Vector2 knockbackDir = ((Vector2)hit.transform.position - (Vector2)transform.position).normalized;
                target.TakeDamage(weaponData.Damage, knockbackDir, weaponData.KnockbackForce);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (weaponData != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, weaponData.AoeRadius);
        }
    }
}