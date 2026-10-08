using System.Collections;
using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// VFXAutoDespawn is a Unity MonoBehaviour that automatically despawns a visual effect (VFX) GameObject after a specified lifetime.
/// It can either return the GameObject to an object pool or deactivate it if no pool is set.
/// This is useful for managing temporary visual effects in a game, ensuring they are cleaned up after use without manual intervention.
/// </summary>
public class VFXAutoDespawn : MonoBehaviour
{
    [Tooltip("How long this effect lasts before returning to the pool.")]
    [SerializeField] private float lifetime = 1.5f;

    private IObjectPool<GameObject> originPool;

    public void SetPool(IObjectPool<GameObject> pool)
    {
        originPool = pool;
    }

    private void OnEnable()
    {
        StartCoroutine(DespawnRoutine());
    }

    private IEnumerator DespawnRoutine()
    {
        yield return new WaitForSeconds(lifetime);

        if (originPool != null)
        {
            originPool.Release(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}