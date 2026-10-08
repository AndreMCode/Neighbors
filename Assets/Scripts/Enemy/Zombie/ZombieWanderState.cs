using UnityEngine;

public class ZombieWanderState : EnemyState
{
    private Vector2 currentDir;
    private bool wasWallOnLeft;

    // Raycast and evaluation settings
    private readonly float castDistance = 0.5f;    // Distance to check ahead/side
    private readonly float checkInterval = 0.15f;  // Check rate
    private static readonly Vector2[] Directions = { Vector2.up, Vector2.down, Vector2.left, Vector2.right };
    private float checkTimer;

    public ZombieWanderState(EnemyStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        // Pick a random starting cardinal direction (N, S, E, W)
        currentDir = GetRandomCardinalDirection();
        checkTimer = 0f;
        wasWallOnLeft = false;
    }

    public override void Tick()
    {
        // Transition Check: Switch to Chase if player enters aggro range
        if (stateMachine.IsTargetInAggroRange)
        {
            stateMachine.SwitchState(stateMachine.ZChaseState);
            return;
        }

        // Periodic Left-Hand Rule Navigation Check
        checkTimer += Time.deltaTime;
        if (checkTimer >= checkInterval)
        {
            checkTimer = 0f;
            EvaluateNavigation();
        }
    }

    public override void FixedTick()
    {
        // Move strictly in the current cardinal direction
        Vector2 moveVelocity = currentDir * stateMachine.Data.WanderSpeed;

        // Apply movement plus knockback velocity if any
        stateMachine.Rigidbody.linearVelocity = moveVelocity + stateMachine.KnockbackVelocity;
    }

    public override void Exit()
    {
        stateMachine.Rigidbody.linearVelocity = Vector2.zero;
    }

    private void EvaluateNavigation()
    {
        Vector2 leftDir = GetLeftDirection(currentDir);

        bool wallInFront = CheckWall(currentDir);
        bool wallOnLeft = CheckWall(leftDir);

        if (wallInFront)
        {
            // Front is blocked -> turn right 90 degrees
            currentDir = GetRightDirection(currentDir);
        }
        else if (wasWallOnLeft && !wallOnLeft)
        {
            // We were hugging a left wall and it just ended -> turn left to round the corner
            currentDir = leftDir;
        }

        // Remember left wall state for the next check
        wasWallOnLeft = wallOnLeft;
    }

    private bool CheckWall(Vector2 direction)
    {
        if (stateMachine.Collider == null) return false;

        // Calculate origin based on Collider offset
        Vector2 origin = (Vector2)stateMachine.transform.position + stateMachine.Collider.offset;

        float circleSize = stateMachine.Collider.radius;

        RaycastHit2D hit = Physics2D.CircleCast(
            origin,
            circleSize,
            direction,
            castDistance,
            stateMachine.Data.ObstacleLayerMask
        );

        return hit.collider != null;
    }

    // 2D Vector Direction Helpers for 90-degree rotations
    private Vector2 GetLeftDirection(Vector2 dir)
    {
        return new Vector2(-dir.y, dir.x);
    }

    private Vector2 GetRightDirection(Vector2 dir)
    {
        return new Vector2(dir.y, -dir.x);
    }

    private Vector2 GetRandomCardinalDirection()
    {
        return Directions[Random.Range(0, Directions.Length)];
    }
}