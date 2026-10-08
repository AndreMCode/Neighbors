using System.Collections;
using UnityEngine;

[RequireComponent(typeof(HealthComponent), typeof(Collider2D))]
public class Decoy : MonoBehaviour
{
    [Header("Lifetime")]
    [Tooltip("How long the decoy remians active before self-destructing (seconds).")]
    [SerializeField] private float lifetime = 10f;

    [Header("Visual Feedback (Optional)")]
    [SerializeField] private GameObject despawnVfxPrefab;

    private HealthComponent health;
    private Coroutine lifetimeCoroutine;
    private bool isUnregistered = false;

    private void Awake()
    {
        health = GetComponent<HealthComponent>();
    }

    private void Start()
    {
        // Register with GameStateManager as a valid enemy target
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.RegisterDecoy(this);
        }

        // Listen for death from enemy contact damage
        if (health != null)
        {
            health.OnDeath += HandleDeath;
        }

        // Begin lifetime countdown
        lifetimeCoroutine = StartCoroutine(LifetimeRoutine());
    }

    private IEnumerator LifetimeRoutine()
    {
        yield return new WaitForSeconds(lifetime);
        Expire();
    }

    private void HandleDeath()
    {
        Expire();
    }

    private void Expire()
    {
        if (isUnregistered) return;
        isUnregistered = true;

        if (lifetimeCoroutine != null)
        {
            StopCoroutine(lifetimeCoroutine);
        }

        // Unregister from GameStateManager
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.UnregisterDecoy(this);
        }

        if (despawnVfxPrefab != null)
        {
            Instantiate(despawnVfxPrefab, transform.position, Quaternion.identity);
        }

        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (health != null)
        {
            health.OnDeath -= HandleDeath;
        }

        // Safety fallback if destroyed externally
        if (!isUnregistered && GameStateManager.Instance != null)
        {
            GameStateManager.Instance.UnregisterDecoy(this);
        }
    }
}