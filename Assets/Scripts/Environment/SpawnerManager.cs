using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages all EnemySpawner instances in the scene, enabling or disabling
/// them based on playerproximity and a global enemy budget.
/// </summary>
public class SpawnerManager : MonoBehaviour
{
    public static SpawnerManager Instance { get; private set; }

    [Header("Player Tracking")]
    [Tooltip("Reference to the player. If unassigned, automatically located by 'Player' tag.")]
    [SerializeField] private Transform playerTransform;

    [Header("Distance Thresholds (Hysteresis)")]
    [Tooltip("Distance from player at which an inactive spawner turns ON.")]
    [SerializeField] private float activationDistance = 8f;

    [Tooltip("Distance from player at which an active spawner turns OFF.")]
    [SerializeField] private float deactivationDistance = 12f;

    [Header("Global Budget")]
    [Tooltip("Maximum total enemies allowed to be active concurrently across all spawners.")]
    [SerializeField] private int maxGlobalEnemies = 35;

    [Tooltip("How frequently (in seconds) spawners are evaluated against player proximity.")]
    [SerializeField] private float evaluationInterval = 0.5f;

    [Header("Auto Discovery")]
    [Tooltip("If true, automatically finds all EnemySpawner components in the scene on Awake.")]
    [SerializeField] private bool autoDiscoverSpawners = true;

    private readonly List<EnemySpawner> registeredSpawners = new List<EnemySpawner>();
    private int currentGlobalEnemyCount = 0;
    private Coroutine evaluationCoroutine;

    public Transform PlayerTransform => playerTransform;
    public bool CanSpawnGlobal => currentGlobalEnemyCount < maxGlobalEnemies;

    public int CurrentEnemyCount
    {
        get
        {
            if (EnemyPoolManager.Instance != null)
            {
                return EnemyPoolManager.Instance.TotalActiveEnemies;
            }
            return 0;
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        LocatePlayer();

        if (autoDiscoverSpawners)
        {
            EnemySpawner[] found = FindObjectsByType<EnemySpawner>(FindObjectsInactive.Include);
            foreach (var spawner in found)
            {
                RegisterSpawner(spawner);
            }
        }
    }

    private void Start()
    {
        EvaluateSpawners();
        evaluationCoroutine = StartCoroutine(ProximityEvaluationRoutine());
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void RegisterSpawner(EnemySpawner spawner)
    {
        if (spawner != null && !registeredSpawners.Contains(spawner))
        {
            registeredSpawners.Add(spawner);
        }
    }

    public void UnregisterSpawner(EnemySpawner spawner)
    {
        registeredSpawners.Remove(spawner);
    }

    public void RegisterSpawnedEnemy()
    {
        currentGlobalEnemyCount++;
    }

    public void UnregisterSpawnedEnemy()
    {
        currentGlobalEnemyCount--;
        if (currentGlobalEnemyCount < 0) currentGlobalEnemyCount = 0;
    }

    public void SetPlayerTransform(Transform player)
    {
        playerTransform = player;
    }

    private void LocatePlayer()
    {
        if (playerTransform != null) return;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }
    }

    private IEnumerator ProximityEvaluationRoutine()
    {
        WaitForSeconds wait = new(evaluationInterval);

        while (true)
        {
            yield return wait;
            EvaluateSpawners();
        }
    }

    public void EvaluateSpawners()
    {
        if (playerTransform == null)
        {
            LocatePlayer();
            if (playerTransform == null) return;
        }

        Vector2 playerPos = playerTransform.position;
        float actDistSqr = activationDistance * activationDistance;
        float deactDistSqr = deactivationDistance * deactivationDistance;

        for (int i = registeredSpawners.Count - 1; i >= 0; i--)
        {
            EnemySpawner spawner = registeredSpawners[i];
            if (spawner == null)
            {
                registeredSpawners.RemoveAt(i);
                continue;
            }

            // Only toggle if the GameObject itself is active
            if (!spawner.gameObject.activeInHierarchy) continue;

            float sqrDist = ((Vector2)spawner.transform.position - playerPos).sqrMagnitude;

            if (!spawner.enabled && sqrDist <= actDistSqr)
            {
                spawner.enabled = true;
            }
            else if (spawner.enabled && sqrDist > deactDistSqr)
            {
                spawner.enabled = false;
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 center = playerTransform != null ? playerTransform.position : transform.position;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(center, activationDistance);

        Gizmos.color = new Color(0f, 0.5f, 1f, 0.5f);
        Gizmos.DrawWireSphere(center, deactivationDistance);
    }
}
