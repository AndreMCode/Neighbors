using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class VFXPoolManager : MonoBehaviour
{
    public static VFXPoolManager Instance { get; private set; }

    [Header("Pool Settings")]
    [SerializeField] private int defaultCapacity = 20;
    [SerializeField] private int maxPoolSize = 100;

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

    public GameObject SpawnVFX(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null) return null;

        IObjectPool<GameObject> pool = GetOrCreatePool(prefab);
        GameObject instance = pool.Get();
        instance.transform.SetPositionAndRotation(position, rotation);

        // Inject the pool reference so the VFX can return itself when finished
        if (instance.TryGetComponent(out VFXAutoDespawn vfxDespawn))
        {
            vfxDespawn.SetPool(pool);
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