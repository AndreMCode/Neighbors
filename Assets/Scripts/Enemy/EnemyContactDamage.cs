using UnityEngine;

[RequireComponent(typeof(EnemyStateMachine), typeof(Collider2D))]
public class EnemyContactDamage : MonoBehaviour
{
    [Header("Damage Settings")]
    [SerializeField] private float damageAmount = 10f;
    [SerializeField] private float knockbackForce = 8f;
    [SerializeField] private LayerMask targetLayer;

    private EnemyStateMachine stateMachine;

    private void Awake()
    {
        stateMachine = GetComponent<EnemyStateMachine>();
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        EvaluateContact(collision.gameObject);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        EvaluateContact(collision.gameObject);
    }

    private void EvaluateContact(GameObject targetObj)
    {
        // Layer check mask
        if (((1 << targetObj.layer) & targetLayer) == 0) return;

        // Verify target implements IDamageable
        if (targetObj.TryGetComponent(out IDamageable damageable))
        {
            // Calculate knockback direction away from the enemy's center
            Vector2 knockbackDirection = (targetObj.transform.position - transform.position).normalized;

            // Pull damage amount dynamically from EnemyData if available, fallback to serializable field
            float damage = (stateMachine != null && stateMachine.Data != null)
                ? stateMachine.Data.Damage
                : damageAmount;

            damageable.TakeDamage(damage, knockbackDirection, knockbackForce);
        }
    }
}