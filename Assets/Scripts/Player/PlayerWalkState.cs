using UnityEngine;

public class PlayerWalkState : PlayerState
{
    public PlayerWalkState(PlayerStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        
    }

    public override void Tick()
    {
        // If the player lets go of the D-pad/Left-Stick, revert to Idle
        if (stateMachine.InputReader.MovementValue.sqrMagnitude <= 0.01f)
        {
            stateMachine.SwitchState(stateMachine.IdleState);
            return;
        }

        // Force return to Idle if firing a weapon that disallows run-and-gun
        if (stateMachine.InputReader.IsFiring && !stateMachine.CanMoveWhileFiring)
        {
            stateMachine.SwitchState(stateMachine.IdleState);
        }
    }

    public override void FixedTick()
    {
        Vector2 moveDirection = stateMachine.InputReader.MovementValue.normalized;

        Vector2 moveVelocity = moveDirection * stateMachine.CurrentMoveSpeed;
        stateMachine.Rigidbody.linearVelocity = moveVelocity + stateMachine.KnockbackVelocity;
    }

    public override void Exit()
    {
        
    }
}