using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class ProjectilePoolManager : MonoBehaviour
{
    public static ProjectilePoolManager Instance { get; private set; }

    [Header("Pool Default Settings")]
    [SerializeField] private int defaultCapacity = 20;
    [SerializeField] private int maxPoolSize = 100;

    // Dictionary mapping each projectile prefab to its own ObjectPool
    private readonly Dictionary<GameObject, IObjectPool<GameObject>> pools = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public GameObject SpawnProjectile(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null)
        {
            Debug.LogError("ProjectilePoolManager: Cannot spawn projectile from a null prefab!");
            return null;
        }

        // Get or create the pool associated with this prefab
        IObjectPool<GameObject> pool = GetOrCreatePool(prefab);

        GameObject instance = pool.Get();
        instance.transform.SetPositionAndRotation(position, rotation);

        // Inject the pool reference into the projectile script so it can return itself
        if (instance.TryGetComponent(out Projectile projectile))
        {
            projectile.SetPool(pool);
        }

        return instance;
    }

    private IObjectPool<GameObject> GetOrCreatePool(GameObject prefab)
    {
        if (pools.TryGetValue(prefab, out var existingPool))
        {
            return existingPool;
        }

        IObjectPool<GameObject> newPool = new ObjectPool<GameObject>(
            createFunc: () => Instantiate(prefab, transform),
            actionOnGet: (obj) => obj.SetActive(true),
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