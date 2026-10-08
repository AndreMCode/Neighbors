using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class EnemyPoolManager : MonoBehaviour
{
    public static EnemyPoolManager Instance { get; private set; }

    [Header("Pool Capacity Default Settings")]
    [SerializeField] private int defaultCapacity = 10;
    [SerializeField] private int maxPoolSize = 50;

    private readonly Dictionary<GameObject, ObjectPool<GameObject>> pools = new();

    public int TotalActiveEnemies
    {
        get
        {
            int total = 0;
            foreach (var pool in pools.Values)
            {
                total += pool.CountActive;
            }
            return total;
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
    }

    public GameObject SpawnEnemy(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null)
        {
            Debug.LogError("EnemyPoolManager: Attempted to spawn a null enemy prefab!");
            return null;
        }

        IObjectPool<GameObject> pool = GetOrCreatePool(prefab);
        GameObject instance = pool.Get();
        instance.transform.SetPositionAndRotation(position, rotation);

        // Inject the pool reference into the enemy state machine for self-recycling
        if (instance.TryGetComponent(out EnemyStateMachine stateMachine))
        {
            stateMachine.SetPool(pool);
        }

        return instance;
    }

    private ObjectPool<GameObject> GetOrCreatePool(GameObject prefab)
    {
        if (pools.TryGetValue(prefab, out var existingPool))
        {
            return existingPool;
        }

        ObjectPool<GameObject> newPool = new(
        createFunc: () => Instantiate(prefab, transform),
        actionOnGet: (obj) =>
        {
            obj.SetActive(true);
            // Factory Reset call when object comes out of pool
            if (obj.TryGetComponent(out EnemyStateMachine stateMachine))
            {
                stateMachine.ResetToDefaultState();
            }
        },
        actionOnRelease: (obj) => obj.SetActive(false),
        actionOnDestroy: (obj) => Destroy(obj),
        collectionCheck: false,
        defaultCapacity: defaultCapacity,
        maxSize: maxPoolSize
    );

        pools.Add(prefab, newPool);
        return newPool;
    }
}