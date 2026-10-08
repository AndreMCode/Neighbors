using Unity.VisualScripting;
using UnityEngine;

public class ZombieSpawnState : EnemyState
{
    private readonly float spawnDuration = 1f;
    private readonly float flickerInterval = 0.04f;

    private float spawnTimer;
    private float flickerTimer;

    private SpriteRenderer spriteRenderer;
    private Collider2D[] colliders;

    public ZombieSpawnState(EnemyStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        // Freeze velocity during spawn sequence
        stateMachine.Rigidbody.linearVelocity = Vector2.zero;

        // Lazy cache component references on first enter
        if (spriteRenderer == null)
        {
            spriteRenderer = stateMachine.GetComponentInChildren<SpriteRenderer>();
        }

        if (colliders == null)
        {
            // Retrieves both the root CircleCollider2D and any child BoxCollider2Ds
            colliders = stateMachine.GetComponentsInChildren<Collider2D>(true);
        }

        // Disable all colliders (root circle collider + child hitbox box colliders)
        SetCollidersEnabled(false);

        spawnTimer = 0f;
        flickerTimer = 0f;
    }

    public override void Tick()
    {
        spawnTimer += Time.deltaTime;
        flickerTimer += Time.deltaTime;

        // Toggle sprite visibility for spawning flicker effect
        if (spriteRenderer != null && flickerTimer >= flickerInterval)
        {
            flickerTimer = 0f;
            spriteRenderer.enabled = !spriteRenderer.enabled;
        }

        // Transition to wander state after spawn duration finishes
        if (spawnTimer >= spawnDuration)
        {
            stateMachine.SwitchState(stateMachine.ZWanderState);
        }
    }

    public override void FixedTick() { }

    public override void Exit()
    {
        // Re-enable all colliders when exiting spawn state
        SetCollidersEnabled(true);

        // Ensure sprite is left visible
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
        }
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