using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum SpawnerShape
{
    Rectangle,
    Circle
}

public class EnemySpawner : MonoBehaviour
{
    [Header("Area Geometry")]
    [SerializeField] private SpawnerShape shape = SpawnerShape.Rectangle;
    [SerializeField] private Vector2 rectangleSize = new(10f, 10f);
    [SerializeField] private float circleRadius = 5f;

    [Header("Spawn Configuration")]
    [SerializeField] private List<GameObject> enemyPrefabs = new();
    [SerializeField] private int maxSimultaneousEnemies = 5;
    [SerializeField] private float spawnInterval = 3f;
    [SerializeField] private int maxPlacementAttempts = 10;

    [Header("Clearance & Obstacles")]
    [SerializeField] private float clearanceRadius = 0.5f;
    [SerializeField] private LayerMask obstacleLayerMask;

    private List<GameObject> validPrefabs = new();
    private HashSet<GameObject> trackedEnemies = new();
    private Coroutine spawnCoroutine;

    private void Awake()
    {
        validPrefabs = enemyPrefabs.FindAll(p => p != null);
        if (validPrefabs.Count == 0)
        {
            Debug.LogWarning($"EnemySpawner on {gameObject.name}: No valid prefabs assigned in enemyPrefabs list!");
        }
    }

    private void OnEnable()
    {
        GameStateManager.OnGameStateChanged += HandleGameStateChanged;

        spawnCoroutine = StartCoroutine(SpawnRoutine());
    }

    private void OnDisable()
    {
        GameStateManager.OnGameStateChanged -= HandleGameStateChanged;

        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }

        // Forget existing enemies so they don't block spawning upon reactivation
        trackedEnemies.Clear();
    }

    private void HandleGameStateChanged(GameState newState)
    {
        // Stop spawning when the level is complete
        if (newState == GameState.LevelComplete)
        {
            if (spawnCoroutine != null)
            {
                StopCoroutine(spawnCoroutine);
                spawnCoroutine = null;
            }
        }
    }

    public void RemoveTrackedEnemy(GameObject enemy)
    {
        // Safe to call even if the set was cleared during OnDisable
        trackedEnemies.Remove(enemy);
    }

    private IEnumerator SpawnRoutine()
    {
        // Small initial delay to stagger evaluation across spawners
        yield return new WaitForSeconds(Random.Range(0f, 0.5f));
        WaitForSeconds wait = new(spawnInterval);

        while (true)
        {
            bool canSpawnGlobal = SpawnerManager.Instance != null && SpawnerManager.Instance.CanSpawnGlobal;

            if (canSpawnGlobal && validPrefabs.Count > 0 && trackedEnemies.Count < maxSimultaneousEnemies)
            {
                TrySpawnEnemy();
            }

            yield return wait;
        }
    }

    private void TrySpawnEnemy()
    {
        // Verify EnemyPoolManager exists in scene
        if (EnemyPoolManager.Instance == null)
        {
            Debug.Log("EnemySpawner: Premature EnemyPoolManager.Instance lookup.");
            return;
        }

        for (int attempt = 0; attempt < maxPlacementAttempts; attempt++)
        {
            Vector2 candidatePoint = GetRandomPointInShape();

            // Verify the point does not intersect wall/obstacle colliders
            if (!Physics2D.OverlapCircle(candidatePoint, clearanceRadius, obstacleLayerMask))
            {
                GameObject selectedPrefab = validPrefabs[Random.Range(0, validPrefabs.Count)];

                GameObject enemyInstance = EnemyPoolManager.Instance.SpawnEnemy(
                    selectedPrefab,
                    candidatePoint,
                    Quaternion.identity
                );

                if (enemyInstance != null)
                {
                    TrackSpawnedEnemy(enemyInstance);
                }
                break;
            }
        }
    }

    private void TrackSpawnedEnemy(GameObject enemyInstance)
    {
        trackedEnemies.Add(enemyInstance);
        SpawnerManager.Instance.RegisterSpawnedEnemy();

        // Attach tracker to handle automatic unregistration on death/despawn
        if (!enemyInstance.TryGetComponent<SpawnerTrackedEnemy>(out var tracker))
        {
            tracker = enemyInstance.AddComponent<SpawnerTrackedEnemy>();
        }
        tracker.Initialize(this);
    }

    private Vector2 GetRandomPointInShape()
    {
        Vector2 origin = transform.position;

        if (shape == SpawnerShape.Rectangle)
        {
            float randomX = Random.Range(-rectangleSize.x / 2f, rectangleSize.x / 2f);
            float randomY = Random.Range(-rectangleSize.y / 2f, rectangleSize.y / 2f);
            return origin + new Vector2(randomX, randomY);
        }
        else // Circle
        {
            Vector2 randomPointInCircle = Random.insideUnitCircle * circleRadius;
            return origin + randomPointInCircle;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;

        if (shape == SpawnerShape.Rectangle)
        {
            Gizmos.DrawWireCube(transform.position, rectangleSize);
        }
        else
        {
            Gizmos.DrawWireSphere(transform.position, circleRadius);
        }
    }
}