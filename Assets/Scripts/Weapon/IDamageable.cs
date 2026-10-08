using UnityEngine;

public interface IDamageable
{
    void TakeDamage(float damageAmount, Vector2 hitDirection, float knockbackForce = 0f);
}