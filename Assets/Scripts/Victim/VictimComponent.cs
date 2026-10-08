using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class VictimComponent : MonoBehaviour, IDamageable
{
    [Header("Victim Settings")]
    [SerializeField] private int scoreValue = 1000;
    [Header("Rescue Animation")]
    [SerializeField] private float rescueMoveDuration = 0.5f;

    // Static events for audio triggers, VFX, or local listeners
    public static event Action<VictimComponent> OnVictimSpawned;
    public static event Action<VictimComponent> OnVictimCollected; // Touched by player, joins the queue
    public static event Action<VictimComponent, int> OnVictimRescued; // Dropped off at Exit Portal
    public static event Action<VictimComponent> OnVictimKilled;

    public int ScoreValue => scoreValue;
    public bool IsResolved { get; private set; }
    public bool IsQueued { get; private set; }

    private bool isBeingRescued = false;

    private void Start()
    {
        // Register with GameStateManager once on spawn/initialization
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.RegisterVictim(this);
        }

        OnVictimSpawned?.Invoke(this);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (IsResolved) return;

        // Player picks up the victim, adding it to the follower queue
        if (!IsQueued && collision.CompareTag("Player"))
        {
            Collect();
        }
        // Enemy touching the victim directly kills it, whether idle or queued
        else if (collision.CompareTag("Enemy"))
        {
            Kill();
        }
    }

    public void TakeDamage(float damageAmount, Vector2 hitDirection, float knockbackForce = 0f)
    {
        if (IsResolved) return;
        Kill();
    }

    private void Collect()
    {
        if (IsQueued) return;
        IsQueued = true;

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.CollectVictim(this);
        }

        OnVictimCollected?.Invoke(this);
    }

    public void BeginRescue(Vector3 targetPosition)
    {
        if (isBeingRescued || IsResolved) return;

        isBeingRescued = true;
        IsQueued = false;

        StartCoroutine(RescueRoutine(targetPosition));
    }

    private IEnumerator RescueRoutine(Vector3 targetPosition)
    {
        Vector3 startPosition = transform.position;
        float elapsed = 0f;

        while (elapsed < rescueMoveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / rescueMoveDuration);
            transform.position = Vector2.Lerp(startPosition, targetPosition, t);
            yield return null;
        }

        FinalizeRescue();
    }

    private void FinalizeRescue()
    {
        if (IsResolved) return;

        IsResolved = true;
        IsQueued = false;
        isBeingRescued = false;

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.RescueVictim(this, scoreValue);
        }

        OnVictimRescued?.Invoke(this, scoreValue);
        gameObject.SetActive(false);
    }

    public void Kill()
    {
        if (IsResolved) return;

        IsResolved = true;
        IsQueued = false;

        // If an enemy catches the victim mid-rescue-animation, cancel the beeline
        if (isBeingRescued)
        {
            StopAllCoroutines();
            isBeingRescued = false;
        }

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.KillVictim(this);
        }

        OnVictimKilled?.Invoke(this);
        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        // Ensure unregistration if destroyed before resolution
        if (!IsResolved && !IsQueued && GameStateManager.Instance != null)
        {
            GameStateManager.Instance.UnregisterVictim(this);
        }
    }
}