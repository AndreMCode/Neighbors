using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerStateMachine))]
public class VictimQueue : MonoBehaviour
{
    [Header("Queue Settings")]
    [SerializeField] private int maxQueueLength = 3;
    [SerializeField, Tooltip("Distance from the player to the first victim.")]
    private float firstVictimOffset = 0.5f;
    [SerializeField, Tooltip("Distance between each subsequent victim.")]
    private float victimSpacing = 0.5f;
    [SerializeField, Tooltip("How often to drop a breadcrumb. Lower = smoother path.")]
    private float recordThreshold = 0.05f;

    private readonly List<VictimComponent> queuedVictims = new();

    private Vector2[] trailBuffer;
    private int trailHead;   // Physical index of the newest entry
    private int trailCount;  // Number of valid entries currently stored

    private PlayerStateMachine playerStateMachine;

    public int Count => queuedVictims.Count;
    public bool IsFull => queuedVictims.Count >= maxQueueLength;

    private void Awake()
    {
        playerStateMachine = GetComponent<PlayerStateMachine>();

        float maxRequiredDistance = firstVictimOffset + (maxQueueLength * victimSpacing);
        int capacity = Mathf.CeilToInt(maxRequiredDistance / recordThreshold) + 10; // +10 buffer
        trailBuffer = new Vector2[Mathf.Max(capacity, 1)];
        trailHead = 0;
        trailCount = 0;
    }

    private void OnEnable()
    {
        VictimComponent.OnVictimCollected += HandleVictimCollected;
        VictimComponent.OnVictimKilled += HandleVictimKilled;
    }

    private void OnDisable()
    {
        VictimComponent.OnVictimCollected -= HandleVictimCollected;
        VictimComponent.OnVictimKilled -= HandleVictimKilled;
    }

    private void Start()
    {
        PushTrailPoint(playerStateMachine.transform.position);
    }

    private void LateUpdate()
    {
        RecordTrailPosition();
        UpdateVictimPositions();
    }

    private void RecordTrailPosition()
    {
        Vector2 currentPosition = playerStateMachine.transform.position;

        // Ensure we always have a starting point
        if (trailCount == 0)
        {
            PushTrailPoint(currentPosition);
            return;
        }

        // Drop a point when we exceed the threshold
        if ((currentPosition - GetTrailPoint(0)).sqrMagnitude >= recordThreshold * recordThreshold)
        {
            PushTrailPoint(currentPosition);
        }
    }

    private void PushTrailPoint(Vector2 point)
    {
        trailHead = (trailHead + 1) % trailBuffer.Length;
        trailBuffer[trailHead] = point;

        if (trailCount < trailBuffer.Length)
        {
            trailCount++;
        }
        // else: buffer is full, oldest entry was implicitly overwritten/"dropped"
    }

    private Vector2 GetTrailPoint(int logicalIndex)
    {
        int physicalIndex = (trailHead - logicalIndex + trailBuffer.Length) % trailBuffer.Length;
        return trailBuffer[physicalIndex];
    }

    private void UpdateVictimPositions()
    {
        if (queuedVictims.Count == 0 || trailCount == 0) return;

        Vector2 playerPos = playerStateMachine.transform.position;

        for (int i = 0; i < queuedVictims.Count; i++)
        {
            VictimComponent victim = queuedVictims[i];
            if (victim == null) continue;

            float currentTargetDistance = firstVictimOffset + (i * victimSpacing);
            float accumulatedDistance = 0f;
            bool foundPosition = false;

            // --- Segment 0: From live player position to the first breadcrumb ---
            Vector2 p1 = playerPos;
            Vector2 p2 = GetTrailPoint(0);
            float segmentLength = Vector2.Distance(p1, p2);

            if (segmentLength >= currentTargetDistance)
            {
                float t = currentTargetDistance / segmentLength;
                victim.transform.position = Vector2.Lerp(p1, p2, t);
                continue;
            }

            accumulatedDistance += segmentLength;

            // --- Subsequent segments: From breadcrumb to breadcrumb ---
            for (int j = 0; j < trailCount - 1; j++)
            {
                p1 = GetTrailPoint(j);
                p2 = GetTrailPoint(j + 1);
                segmentLength = Vector2.Distance(p1, p2);
                if (accumulatedDistance + segmentLength >= currentTargetDistance)
                {
                    float leftoverDistance = currentTargetDistance - accumulatedDistance;
                    float t = segmentLength > 0f ? leftoverDistance / segmentLength : 0f;
                    victim.transform.position = Vector2.Lerp(p1, p2, t);
                    foundPosition = true;
                    break;
                }
                accumulatedDistance += segmentLength;
            }

            // If the trail history isn't long enough yet (e.g., player just started moving)
            // clamp the victim to the very end of the available trail
            if (!foundPosition)
            {
                victim.transform.position = GetTrailPoint(trailCount - 1);
            }
        }
    }

    public bool TryEnqueue(VictimComponent victim)
    {
        if (victim == null || IsFull || queuedVictims.Contains(victim)) return false;

        queuedVictims.Add(victim);
        return true;
    }

    public List<VictimComponent> DetachAllForRescue()
    {
        var detached = new List<VictimComponent>(queuedVictims);
        queuedVictims.Clear();
        return detached;
    }

    private void HandleVictimCollected(VictimComponent victim)
    {
        TryEnqueue(victim);
    }

    private void HandleVictimKilled(VictimComponent victim)
    {
        queuedVictims.Remove(victim);
    }
}