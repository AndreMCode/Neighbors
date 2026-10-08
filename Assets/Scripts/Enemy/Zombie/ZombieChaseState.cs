using UnityEngine;

public class ZombieChaseState : EnemyState
{
    public ZombieChaseState(EnemyStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        
    }

    public override void Tick()
    {
        // Return to Wander if the player moves beyond the de-aggro radius
        if (!stateMachine.IsTargetInAggroRange)
        {
            stateMachine.SwitchState(stateMachine.ZWanderState);
        }
    }

    public override void FixedTick()
    {
        if (stateMachine.CurrentTarget == null) return;

        // Calculate direct vector to the player
        Vector2 direction = ((Vector2)stateMachine.CurrentTarget.position - (Vector2)stateMachine.transform.position).normalized;

        // Apply movement plus knockback velocity if any
        Vector2 moveVelocity = direction * stateMachine.Data.ChaseSpeed;
        stateMachine.Rigidbody.linearVelocity = moveVelocity + stateMachine.KnockbackVelocity;
    }

    public override void Exit()
    {
        stateMachine.Rigidbody.linearVelocity = Vector2.zero;
    }
}