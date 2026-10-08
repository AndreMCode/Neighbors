using UnityEngine;

public class DollStalkState : EnemyState
{
    private readonly float checkInterval = 0.1f;
    private float checkTimer;

    public DollStalkState(EnemyStateMachine stateMachine) : base(stateMachine) { }

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
            EvaluateSprintTrigger();
        }
    }

    public override void FixedTick()
    {
        if (stateMachine.CurrentTarget == null) return;

        // Slow, direct vector movement toward player
        Vector2 dir = ((Vector2)stateMachine.CurrentTarget.position - (Vector2)stateMachine.transform.position).normalized;

        // Apply movement plus knockback velocity if any
        Vector2 moveVelocity = dir * stateMachine.Data.StalkSpeed;
        stateMachine.Rigidbody.linearVelocity = moveVelocity + stateMachine.KnockbackVelocity;
    }

    public override void Exit()
    {
        stateMachine.Rigidbody.linearVelocity = Vector2.zero;
    }

    private void EvaluateSprintTrigger()
    {
        Vector2 dollPos = stateMachine.transform.position;
        Vector2 playerPos = stateMachine.CurrentTarget.position;
        float threshold = stateMachine.Data.AlignmentThreshold;

        Vector2 sprintDir = Vector2.zero;

        // Check X Alignment (Vertical Sprint: Up or Down)
        if (Mathf.Abs(dollPos.x - playerPos.x) <= threshold)
        {
            sprintDir = (playerPos.y > dollPos.y) ? Vector2.up : Vector2.down;
        }
        // Check Y Alignment (Horizontal Sprint: Left or Right)
        else if (Mathf.Abs(dollPos.y - playerPos.y) <= threshold)
        {
            sprintDir = (playerPos.x > dollPos.x) ? Vector2.right : Vector2.left;
        }

        // If cardinally aligned, verify clear Line of Sight before sprinting
        if (sprintDir != Vector2.zero && HasCardinalLOS(sprintDir, playerPos))
        {
            stateMachine.DSprintState.SetDirection(sprintDir);
            stateMachine.SwitchState(stateMachine.DSprintState);
        }
    }

    private bool HasCardinalLOS(Vector2 direction, Vector2 playerPos)
    {
        Vector2 dollPos = stateMachine.transform.position;
        float distance = Vector2.Distance(dollPos, playerPos);

        RaycastHit2D hit = Physics2D.Raycast(
            dollPos,
            direction,
            distance,
            stateMachine.Data.ObstacleLayerMask
        );

        return hit.collider == null;
    }
}