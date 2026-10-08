using UnityEngine;

public class PlayerDeathState : PlayerState
{
    public static event System.Action OnPlayerDied;

    public PlayerDeathState(PlayerStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        // Zero out movement and freeze physics
        stateMachine.Rigidbody.linearVelocity = Vector2.zero;
        stateMachine.Rigidbody.simulated = false;

        // Broadcast player death for game over screens / life counters
        OnPlayerDied?.Invoke();
    }

    public override void Tick() { }
    public override void FixedTick() { }
    public override void Exit() { }
}