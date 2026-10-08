using UnityEngine;

public class PlayerDashState : PlayerState
{
    private float dashTimer;
    private Vector2 dashDirection;

    public PlayerDashState(PlayerStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        dashTimer = stateMachine.DashDuration;
        dashDirection = stateMachine.FacingDirection;

        // Trigger dash start VFX/animation (e.g., trail renderer, dash pose) here

        stateMachine.Rigidbody.linearVelocity = dashDirection * stateMachine.DashSpeed;
    }

    public override void Tick()
    {
        dashTimer -= Time.deltaTime;

        if (dashTimer <= 0f)
        {
            // Dash finished - return to whichever state matches current input
            if (stateMachine.InputReader.MovementValue.sqrMagnitude > 0.01f)
            {
                stateMachine.SwitchState(stateMachine.WalkState);
            }
            else
            {
                stateMachine.SwitchState(stateMachine.IdleState);
            }
        }
    }

    public override void FixedTick()
    {
        // Maintain constant dash velocity for the full duration, ignoring movement input
        stateMachine.Rigidbody.linearVelocity = dashDirection * stateMachine.DashSpeed;
    }

    public override void Exit()
    {
        // Trigger dash end VFX/animation (e.g., stop trail renderer) here

        // Cooldown begins once the dash has fully ended
        stateMachine.StartDashCooldown();
    }
}