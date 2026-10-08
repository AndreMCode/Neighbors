using UnityEngine;

public class PlayerGoalState : PlayerState
{
    private Collider2D[] colliders;

    public PlayerGoalState(PlayerStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        stateMachine.Rigidbody.linearVelocity = Vector2.zero;

        if (colliders == null)
        {
            // Retrieves both the root CircleCollider2D and any child BoxCollider2Ds
            colliders = stateMachine.GetComponentsInChildren<Collider2D>(true);
        }

        // Disable all colliders (root circle collider + child hitbox box colliders)
        SetCollidersEnabled(false);
    }

    public override void Tick() { }

    public override void FixedTick() { }

    public override void Exit()
    {
        // Re-enable all colliders when exiting spawn state
        SetCollidersEnabled(true);
    }

    private void SetCollidersEnabled(bool isEnabled)
    {
        if (colliders == null) return;

        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
            {
                colliders[i].enabled = isEnabled;
            }
        }
    }
}
