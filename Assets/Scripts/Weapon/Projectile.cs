using UnityEngine;
using UnityEngine.Pool;

public abstract class Projectile : MonoBehaviour
{
    protected WeaponData weaponData;
    private IObjectPool<GameObject> originPool;

    public void SetPool(IObjectPool<GameObject> pool)
    {
        originPool = pool;
    }

    public abstract void Initialize(WeaponData data, Vector2 fireDirection);

    protected void ReleaseToPool()
    {
        if (originPool != null)
        {
            originPool.Release(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}