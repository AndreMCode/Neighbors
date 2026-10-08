using UnityEngine;

public class DollSprintState : EnemyState
{
    private Vector2 sprintDirection;
    private readonly float checkInterval = 0.05f;
    private float checkTimer;

    public DollSprintState(EnemyStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        checkTimer = 0f;
    }

    public override void Tick()
    {
        if (stateMachine.CurrentTarget == null) return;

        checkTimer += Time.deltaTime;
        if (checkTimer >= checkInterval)
        {
            checkTimer = 0f;
            if (ShouldCancelSprint())
            {
                stateMachine.SwitchState(stateMachine.DStalkState);
            }
        }
    }

    public override void FixedTick()
    {
        // High-speed cardinal movement locked to sprintDirection
        Vector2 moveVelocity = sprintDirection * stateMachine.Data.SprintSpeed;

        // Apply movement plus knockback velocity if any
        stateMachine.Rigidbody.linearVelocity = moveVelocity + stateMachine.KnockbackVelocity;
    }

    public override void Exit()
    {
        stateMachine.Rigidbody.linearVelocity = Vector2.zero;
    }

    public void SetDirection(Vector2 direction)
    {
        sprintDirection = direction;
    }

    private bool ShouldCancelSprint()
    {
        Vector2 dollPos = stateMachine.transform.position;
        Vector2 playerPos = stateMachine.CurrentTarget.position;
        float threshold = stateMachine.Data.AlignmentThreshold;

        // Check if Player left the cardinal alignment axis
        if (sprintDirection == Vector2.up || sprintDirection == Vector2.down)
        {
            if (Mathf.Abs(dollPos.x - playerPos.x) > threshold) return true;
        }
        else if (sprintDirection == Vector2.left || sprintDirection == Vector2.right)
        {
            if (Mathf.Abs(dollPos.y - playerPos.y) > threshold) return true;
        }

        // Check if a wall blocked the sprint path
        float distance = Vector2.Distance(dollPos, playerPos);
        RaycastHit2D hit = Physics2D.Raycast(
            dollPos,
            sprintDirection,
            Mathf.Min(distance, 1.0f), // Check immediate front path
            stateMachine.Data.ObstacleLayerMask
        );

        return hit.collider != null;
    }
}