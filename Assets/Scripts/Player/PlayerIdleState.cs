using UnityEngine;

public class PlayerIdleState : PlayerState
{
    public PlayerIdleState(PlayerStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        
    }

    public override void Tick()
    {
        // Only transition to Walk if there is movement input AND firing doesn't restrict movement
        if (stateMachine.InputReader.MovementValue.sqrMagnitude > 0.01f)
        {
            if (stateMachine.InputReader.IsFiring && !stateMachine.CanMoveWhileFiring)
            {
                return; // Block walking while firing stationary weapons
            }

            stateMachine.SwitchState(stateMachine.WalkState);
        }
    }

    public override void FixedTick()
    {
        // Enforce zero velocity while idle or firing in place, but still apply knockback if present
        stateMachine.Rigidbody.linearVelocity = stateMachine.KnockbackVelocity;
    }

    public override void Exit()
    {
        
    }
}