using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Pool;

[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D), typeof(HealthComponent))]
public class EnemyStateMachine : MonoBehaviour
{
    [Header("Configuration Asset")]
    [SerializeField] private EnemyData data;

    // Public state properties exposed to Child States
    public EnemyData Data => data;
    public Rigidbody2D Rigidbody { get; private set; }
    public CircleCollider2D Collider { get; private set; }

    public ZombieSpawnState ZSpawnState { get; private set; }
    public ZombieWanderState ZWanderState { get; private set; }
    public ZombieChaseState ZChaseState { get; private set; }

    public DollSpawnState DSpawnState { get; private set; }
    public DollStalkState DStalkState { get; private set; }
    public DollSprintState DSprintState { get; private set; }

    public EnemyDeathState DeathState { get; private set; }

    // Abstracted target properties
    public Transform CurrentTarget { get; private set; }
    public bool IsTargetInAggroRange { get; private set; }
    public bool HasLineOfSight { get; private set; }
    public HealthComponent Health { get; private set; }

    // Knockback
    private Vector2 knockbackVelocity = Vector2.zero;
    public Vector2 KnockbackVelocity => knockbackVelocity;
    private readonly float knockbackDampTime = 0.2f; // Duration knockback persists
    private float knockbackDampElapsed = 0f;

    public event Action<EnemyStateMachine> OnDespawned;

    private EnemyState currentState;
    private Coroutine detectionCoroutine;
    private Transform playerTransform;
    private IObjectPool<GameObject> originPool;

    private void Awake()
    {
        if (data != null)
        {
            if (data.EnemyType == EnemyType.Zombie)
            {
                ZSpawnState = new ZombieSpawnState(this);
                ZWanderState = new ZombieWanderState(this);
                ZChaseState = new ZombieChaseState(this);
            }
            else if (data.EnemyType == EnemyType.ToyDoll)
            {
                DSpawnState = new DollSpawnState(this);
                DStalkState = new DollStalkState(this);
                DSprintState = new DollSprintState(this);
            }
        }

        DeathState = new EnemyDeathState(this);

        Rigidbody = GetComponent<Rigidbody2D>();
        Collider = GetComponent<CircleCollider2D>();
        Health = GetComponent<HealthComponent>();
    }

    private void OnEnable()
    {
        if (Health != null)
        {
            Health.OnDamaged += HandleKnockback;
            Health.OnDeath += HandleEnemyDeath;
        }

        GameStateManager.OnGameStateChanged += HandleGameStateChanged;
    }

    private void OnDisable()
    {
        if (Health != null)
        {
            Health.OnDamaged -= HandleKnockback;
            Health.OnDeath -= HandleEnemyDeath;
        }

        GameStateManager.OnGameStateChanged -= HandleGameStateChanged;

        if (detectionCoroutine != null) StopCoroutine(detectionCoroutine);
    }

    private void Start()
    {
        // Cache the player reference once
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }

        detectionCoroutine = StartCoroutine(DetectionCheckRoutine());

        EnemyState initialState = EnemyStateFactory.CreateInitialState(this);
        if (initialState != null)
        {
            SwitchState(initialState);
        }
    }

    private void Update()
    {
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

    public void SwitchState(EnemyState newState)
    {
        currentState?.Exit();
        currentState = newState;
        currentState?.Enter();
    }

    private void HandleKnockback(Vector2 hitDirection, float knockbackForce)
    {
        knockbackVelocity = hitDirection.normalized * knockbackForce;
        knockbackDampElapsed = 0f; // Reset the damp timer
    }

    public void ResetToDefaultState()
    {
        // Re-enable Physics and Colliders disabled during EnemyDeathState
        if (Collider != null) Collider.enabled = true;
        if (Rigidbody != null)
        {
            Rigidbody.simulated = true;
            Rigidbody.linearVelocity = Vector2.zero;
        }

        // Reset Health and i-Frames
        if (Health != null)
        {
            Health.ResetHealth();
        }

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }

        detectionCoroutine = StartCoroutine(DetectionCheckRoutine());

        // Re-enter default Idle AI state
        EnemyState initialState = EnemyStateFactory.CreateInitialState(this);
        if (initialState != null)
        {
            SwitchState(initialState);
        }
    }

    public void SetPool(IObjectPool<GameObject> pool)
    {
        originPool = pool;
    }

    public void ReleaseToPool()
    {
        OnDespawned?.Invoke(this);

        if (originPool != null)
        {
            originPool.Release(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private IEnumerator DetectionCheckRoutine()
    {
        WaitForSeconds wait = new(data.CheckInterval);

        while (true)
        {
            UpdateClosestTarget();

            if (CurrentTarget != null)
            {
                float sqrDistance = (CurrentTarget.position - transform.position).sqrMagnitude;

                // Proximity Check with Hysteresis (Aggro vs DeAggro)
                if (!IsTargetInAggroRange)
                {
                    if (sqrDistance <= data.SqrAggroRadius) IsTargetInAggroRange = true;
                }
                else
                {
                    if (sqrDistance > data.SqrDeAggroRadius)
                    {
                        IsTargetInAggroRange = false;
                        HasLineOfSight = false;
                    }
                }

                // Line of Sight Check (Only execute if within aggro range)
                if (IsTargetInAggroRange)
                {
                    float distance = Mathf.Sqrt(sqrDistance);
                    Vector2 directionToTarget = (CurrentTarget.position - transform.position).normalized;

                    RaycastHit2D hit = Physics2D.Raycast(
                        transform.position,
                        directionToTarget,
                        distance,
                        data.ObstacleLayerMask
                    );

                    HasLineOfSight = (hit.collider == null);
                }
            }
            else
            {
                // No targets exist (Player dead, all victims dead)
                IsTargetInAggroRange = false;
                HasLineOfSight = false;
            }

            yield return wait;
        }
    }

    private void UpdateClosestTarget()
    {
        Transform bestTarget = null;
        float closestSqrDist = float.MaxValue;

        // Check Player distance
        if (playerTransform != null)
        {
            bestTarget = playerTransform;
            closestSqrDist = (playerTransform.position - transform.position).sqrMagnitude;
        }

        // Check Victim distance
        if (GameStateManager.Instance != null)
        {
            VictimComponent closestVictim = GameStateManager.Instance.GetClosestVictim(transform.position);
            if (closestVictim != null)
            {
                float victimSqrDist = (closestVictim.transform.position - transform.position).sqrMagnitude;
                if (victimSqrDist < closestSqrDist)
                {
                    bestTarget = closestVictim.transform;
                    closestSqrDist = victimSqrDist;
                }
            }

            // Check Decoy distance (distracts enemies if closer than other targets)
            Decoy closestDecoy = GameStateManager.Instance.GetClosestDecoy(transform.position);
            if (closestDecoy != null)
            {
                float decoySqrDist = (closestDecoy.transform.position - transform.position).sqrMagnitude;
                if (decoySqrDist < closestSqrDist)
                {
                    bestTarget = closestDecoy.transform;
                    closestSqrDist = decoySqrDist;
                }
            }
        }

        CurrentTarget = bestTarget;
    }

    private void HandleEnemyDeath()
    {
        // Stop AI detection logic immediately
        if (detectionCoroutine != null) StopCoroutine(detectionCoroutine);

        // Switch to generic death state or deactivate/recycle
        SwitchState(new EnemyDeathState(this));
    }

    private void HandleGameStateChanged(GameState newState)
    {
        // Clear player reference when level is complete
        if (newState == GameState.LevelComplete)
        {
            playerTransform = null;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (data == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, data.AggroRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, data.DeAggroRadius);
    }
}