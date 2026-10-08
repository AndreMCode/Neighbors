using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class HealthComponent : MonoBehaviour, IDamageable
{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;

    [Header("Invulnerability (i-Frames)")]
    [SerializeField] private float iFrameDuration = 1f;
    [SerializeField] private float flashInterval = 0.1f;
    [SerializeField] private SpriteRenderer spriteRenderer;

    private float currentHealth;
    private bool isInvulnerable = false;
    private bool wasHealthInitialized = false;
    private Rigidbody2D rb;
    private Coroutine iFrameCoroutine;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsDead => currentHealth <= 0f;

    public event Action<float, float> OnHealthChanged; // (currentHealth, maxHealth)
    public event Action<Vector2, float> OnDamaged;     // (hitDirection, knockbackForce)
    public event Action OnDeath;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }
    }

    private void OnEnable()
    {
        if (!wasHealthInitialized)
        {
            currentHealth = maxHealth;
        }
        isInvulnerable = false;
        if (spriteRenderer != null) spriteRenderer.enabled = true;
    }

    public void InitializeHealth(float startingHealth, float startingMaxHealth)
    {
        maxHealth = startingMaxHealth;
        currentHealth = Mathf.Clamp(startingHealth, 0f, maxHealth);
        isInvulnerable = false;
        wasHealthInitialized = true;
        if (spriteRenderer != null) spriteRenderer.enabled = true;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    // Damage handling with optional knockback
    public void TakeDamage(float damageAmount, Vector2 hitDirection, float knockbackForce = 0f)
    {
        if (IsDead || isInvulnerable) return;

        currentHealth = Mathf.Max(0f, currentHealth - damageAmount);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnDamaged?.Invoke(hitDirection, knockbackForce);

        // Apply physical knockback impulse if Rigidbody2D is present
        if (rb != null && knockbackForce > 0f)
        {
            rb.AddForce(hitDirection.normalized * knockbackForce, ForceMode2D.Impulse);
        }

        if (IsDead)
        {
            Die();
        }
        else if (iFrameDuration > 0f)
        {
            if (iFrameCoroutine != null) StopCoroutine(iFrameCoroutine);
            iFrameCoroutine = StartCoroutine(InvulnerabilityRoutine());
        }
    }

    public void Heal(float amount)
    {
        if (IsDead) return;

        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private void Die()
    {
        OnDeath?.Invoke();
    }

    public void ResetHealth()
    {
        if (iFrameCoroutine != null)
        {
            StopCoroutine(iFrameCoroutine);
            iFrameCoroutine = null;
        }

        currentHealth = maxHealth;
        isInvulnerable = false;

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
        }

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private IEnumerator InvulnerabilityRoutine()
    {
        isInvulnerable = true;
        float elapsed = 0f;

        while (elapsed < iFrameDuration)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = !spriteRenderer.enabled;
            }

            yield return new WaitForSeconds(flashInterval);
            elapsed += flashInterval;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
        }

        isInvulnerable = false;
        iFrameCoroutine = null;
    }
}