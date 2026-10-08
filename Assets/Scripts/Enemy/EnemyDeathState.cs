using UnityEngine;

public class EnemyDeathState : EnemyState
{
    public EnemyDeathState(EnemyStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        // Stop physics and turn off colliders so other entities can pass through
        stateMachine.Rigidbody.linearVelocity = Vector2.zero;
        stateMachine.Collider.enabled = false;

        // Add other VFX here

        // Return to pool via state machine helper
        stateMachine.ReleaseToPool();
    }

    public override void Tick() { }
    public override void FixedTick() { }

    public override void Exit()
    {
        // Re-enable collider if object gets reused/respawned from a pool
        stateMachine.Collider.enabled = true;
    }
}