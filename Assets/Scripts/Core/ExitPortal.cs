using UnityEngine;

public class ExitPortal : MonoBehaviour
{
    [Header("Visual Feedback")]
    [SerializeField] private GameObject visualEffect;
    [Header("Rescue Animation Target")]
    [SerializeField] private Transform rescueTargetPoint; // Defaults to this transform if unset

    private void OnEnable()
    {
        GameStateManager.OnVictimsUpdated += HandleVictimsUpdated;
    }

    private void OnDisable()
    {
        GameStateManager.OnVictimsUpdated -= HandleVictimsUpdated;
    }

    private void Start()
    {
        // Set the initial state of the visual effect based on whether any victims have been rescued
        bool hasRescued = GameStateManager.Instance != null && GameStateManager.Instance.VictimsRescued > 0;
        if (visualEffect != null) visualEffect.SetActive(hasRescued);
    }

    private void HandleVictimsUpdated(int rescued, int total)
    {
        // Update the visual effect based on whether any victims have been rescued
        if (visualEffect != null) visualEffect.SetActive(rescued > 0);
    }

    public void HandleOuterTriggerStay(Collider2D collision)
    {
        // Only proceed if the colliding object is the player
        if (!collision.CompareTag("Player")) return;

        if (!collision.TryGetComponent<VictimQueue>(out var victimQueue) || victimQueue.Count == 0) return;

        // Determine the target position for the rescue animation
        Vector3 targetPosition = rescueTargetPoint != null ? rescueTargetPoint.position : transform.position;
        var victimsToRescue = victimQueue.DetachAllForRescue();

        // Begin the rescue animation for each victim
        foreach (var victim in victimsToRescue)
        {
            if (victim != null)
                victim.BeginRescue(targetPosition);
        }
    }

    public void HandleInnerTriggerEnter(Collider2D collision)
    {
        // Only proceed if the colliding object is the player
        if (!collision.CompareTag("Player")) return;

        // Check if the player has rescued at least one victim before allowing level completion
        if (GameStateManager.Instance == null || GameStateManager.Instance.VictimsRescued <= 0)
        {
            Debug.Log("Rescue at least one victim before exiting!");
            return;
        }

        // Switch the player's state to the goal state if the PlayerStateMachine component is present
        // *** TO DO: Maybe add a confirm prompt here instead of just switching states? ***
        if (collision.TryGetComponent<PlayerStateMachine>(out var psm))
        {
            psm.SwitchState(psm.GoalState);
        }

        GameStateManager.Instance.ChangeState(GameState.LevelComplete);
        Debug.Log("Player entered exit portal! Transitioning to Level Complete screen.");
    }
}