using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(PlayerInventory), typeof(HealthComponent))]
public class PlayerStateMachine : MonoBehaviour
{
    [Header("Movement Stats")]
    public float BaseMoveSpeed = 5f;

    [Header("Dash Stats")]
    [Tooltip("Constant speed applied to the player for the duration of the dash.")]
    public float DashSpeed = 12f;
    [Tooltip("How long (in seconds) the dash lasts.")]
    public float DashDuration = 0.2f;
    [Tooltip("How long (in seconds) the player must wait after a dash ends before dashing again.")]
    public float DashCooldown = 1.5f;
    [Tooltip("Window (in seconds) before the cooldown ends during which an early dash input is buffered and fired automatically once ready.")]
    public float DashBufferWindow = 0.15f;

    [Header("Input Asset Reference")]
    [SerializeField] private InputReader inputReader;
    [Header("Session Data Reference")]
    [SerializeField] private RunSessionDataSO sessionData;

    // State Instances
    public PlayerIdleState IdleState { get; private set; }
    public PlayerWalkState WalkState { get; private set; }
    public PlayerDashState DashState { get; private set; }
    public PlayerDeathState DeathState { get; private set; }
    public PlayerGoalState GoalState { get; private set; }

    // Components
    public Rigidbody2D Rigidbody { get; private set; }
    public PlayerInventory Inventory { get; private set; }
    public HealthComponent Health { get; private set; }
    public float CurrentMoveSpeed { get; private set; }
    public Vector2 FacingDirection { get; private set; } = Vector2.down; // Defaults to facing down

    // Forwarded Properties
    public InputReader InputReader => inputReader;
    public bool CanMoveWhileFiring =>
        sessionData == null || sessionData.CurrentWeapon == null || sessionData.CurrentWeapon.AllowsMovementWhileFiring;
    private bool IsDashReady => Time.time >= dashCooldownEndTime;
    private bool IsStationaryFiringActive => inputReader.IsFiring && !CanMoveWhileFiring;
    private bool CanEnterDashState =>
        currentState != DashState &&
        currentState != DeathState &&
        currentState != GoalState &&
        !IsStationaryFiringActive;

    private PlayerState currentState;
    private Coroutine speedBoostCoroutine;

    // Knockback
    private Vector2 knockbackVelocity = Vector2.zero;
    public Vector2 KnockbackVelocity => knockbackVelocity;
    private readonly float knockbackDampTime = 0.2f; // Duration knockback persists
    private float knockbackDampElapsed = 0f;

    // Dash Cooldown & Buffer
    private float dashCooldownEndTime = -Mathf.Infinity;
    private bool dashBuffered;

    private void Awake()
    {
        // Allocate once per Player lifetime
        IdleState = new PlayerIdleState(this);
        WalkState = new PlayerWalkState(this);
        DashState = new PlayerDashState(this);
        DeathState = new PlayerDeathState(this);
        GoalState = new PlayerGoalState(this);

        Rigidbody = GetComponent<Rigidbody2D>();
        Inventory = GetComponent<PlayerInventory>();
        Health = GetComponent<HealthComponent>();
    }

    private void OnEnable()
    {
        if (Health != null)
        {
            Health.OnDamaged += HandleKnockback;
            Health.OnDeath += HandlePlayerDeath;
        }

        if (inputReader != null)
        {
            inputReader.OnDashPerformed += HandleDashInput;
        }

        // Register with GameStateManager if present
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.RegisterPlayer(this);
        }
    }

    private void OnDisable()
    {
        if (Health != null)
        {
            Health.OnDamaged -= HandleKnockback;
            Health.OnDeath -= HandlePlayerDeath;
        }

        if (inputReader != null)
        {
            inputReader.OnDashPerformed -= HandleDashInput;
        }

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.UnregisterPlayer(this);
        }
    }

    private void Start()
    {
        CurrentMoveSpeed = BaseMoveSpeed;

        // Begin the game in the Idle state
        SwitchState(IdleState);

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.RegisterPlayer(this);
        }
    }

    private void Update()
    {
        UpdateFacingDirection();
        HandleFiring();
        HandleBufferedDash();

        currentState?.Tick();
    }

    private void FixedUpdate()
    {
        // Decay knockback over time
        if (knockbackDampElapsed < knockbackDampTime)
        {
            knockbackDampElapsed += Time.fixedDeltaTime;
            float dampFactor = Mathf.Clamp01(1f - (knockbackDampElapsed / knockbackDampTime));
            knockbackVelocity *= dampFactor;
        }
        else
        {
            knockbackVelocity = Vector2.zero;
        }

        currentState?.FixedTick();
    }

    public void SwitchState(PlayerState newState)
    {
        currentState?.Exit();
        currentState = newState;
        currentState?.Enter();
    }

    private void UpdateFacingDirection()
    {
        // Remembers the last 8-directional input vector when moving
        if (inputReader.MovementValue.sqrMagnitude > 0.01f)
        {
            FacingDirection = inputReader.MovementValue.normalized;
        }
    }

    private void HandleFiring()
    {
        // Weapons cannot be fired while dashing
        if (currentState == DashState)
        {
            return;
        }

        if (inputReader.IsFiring && sessionData != null && sessionData.CurrentWeapon != null)
        {
            // If current weapon doesn't allow run-and-gun, lock movement by stopping velocity
            if (!sessionData.CurrentWeapon.AllowsMovementWhileFiring)
            {
                Rigidbody.linearVelocity = knockbackVelocity;
            }

            Inventory.TryFire(transform.position, FacingDirection);
        }
    }

    private void HandleKnockback(Vector2 hitDirection, float knockbackForce)
    {
        knockbackVelocity = hitDirection.normalized * knockbackForce;
        knockbackDampElapsed = 0f;
    }

    // Dash Readiness; a float property read is cheaper than a method call, and this is polled every frame by the UI
    public float DashReadiness01
    {
        get
        {
            // Bar is fully depleted for the entire duration of the dash itself
            if (currentState == DashState)
            {
                return 0f;
            }

            float cooldownStartTime = dashCooldownEndTime - DashCooldown;
            float elapsed = Time.time - cooldownStartTime;

            return Mathf.Clamp01(elapsed / DashCooldown);
        }
    }

    private void HandleDashInput()
    {
        if (!CanEnterDashState) return;

        if (IsDashReady)
        {
            SwitchState(DashState);
            return;
        }

        // Buffer slightly early input so a dash fires the instant cooldown expires,
        // but only if the request falls within the configured buffer window
        float remainingCooldown = dashCooldownEndTime - Time.time;
        if (remainingCooldown <= DashBufferWindow)
        {
            dashBuffered = true;
        }
    }

    private void HandleBufferedDash()
    {
        if (dashBuffered && IsDashReady && CanEnterDashState)
        {
            dashBuffered = false;
            SwitchState(DashState);
        }
    }

    public void StartDashCooldown()
    {
        dashCooldownEndTime = Time.time + DashCooldown;
    }

    public void ApplySpeedBoost(float multiplier, float duration)
    {
        // If the player uses Zoom Shoes while already zooming, reset the timer
        if (speedBoostCoroutine != null)
        {
            StopCoroutine(speedBoostCoroutine);
        }

        speedBoostCoroutine = StartCoroutine(SpeedBoostRoutine(multiplier, duration));
    }

    private System.Collections.IEnumerator SpeedBoostRoutine(float multiplier, float duration)
    {
        CurrentMoveSpeed = BaseMoveSpeed * multiplier;

        yield return new WaitForSeconds(duration);

        CurrentMoveSpeed = BaseMoveSpeed;
        speedBoostCoroutine = null;
    }

    private void HandlePlayerDeath()
    {
        SwitchState(DeathState);
    }

    public void Respawn(Vector2 spawnPosition)
    {
        // Move to spawn position
        transform.position = spawnPosition;

        // Restore physics and colliders
        Rigidbody.simulated = true;
        Rigidbody.linearVelocity = Vector2.zero;

        // Reset health and trigger invulnerability frames
        if (Health != null)
        {
            Health.ResetHealth();
        }

        // Clear any stale dash buffer/cooldown carried over from before death
        dashBuffered = false;
        dashCooldownEndTime = -Mathf.Infinity;

        // Return control state machine to Idle
        SwitchState(IdleState);
    }
}