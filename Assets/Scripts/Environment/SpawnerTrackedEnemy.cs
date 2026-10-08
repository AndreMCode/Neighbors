using UnityEngine;

/// <summary>
/// This component is attached to enemies spawned by an EnemySpawner. It tracks the origin spawner and
/// ensures that when the enemy is disabled (e.g., destroyed or pooled), it notifies the spawner to
/// remove it from its tracking list. Additionally, it informs the SpawnerManager to update the global enemy count.
/// </summary>
public class SpawnerTrackedEnemy : MonoBehaviour
{
    private EnemySpawner originSpawner;
    private bool isInitialized = false;

    public void Initialize(EnemySpawner spawner)
    {
        originSpawner = spawner;
        isInitialized = true;
    }

    private void OnDisable()
    {
        if (!isInitialized) return;

        if (originSpawner != null)
        {
            originSpawner.RemoveTrackedEnemy(gameObject);
        }

        if (SpawnerManager.Instance != null)
        {
            SpawnerManager.Instance.UnregisterSpawnedEnemy();
        }

        isInitialized = false;
        originSpawner = null;
    }
}