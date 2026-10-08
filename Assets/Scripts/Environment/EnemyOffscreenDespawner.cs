using System.Collections;
using UnityEngine;

[RequireComponent(typeof(EnemyStateMachine))]
public class EnemyOffscreenDespawner : MonoBehaviour
{
    [Header("Viewport Padding Fallback")]
    [Tooltip("Extra viewport-space padding beyond the screen edges before despawning (0 = exactly at screen edge, 0.5 = half a screen beyond edge).")]
    [SerializeField] private float viewportPadding = 0.2f;

    [Header("Detection Interval")]
    [Tooltip("Interval (in seconds) between proximity checks.")]
    [SerializeField] private float checkInterval = 0.5f;

    private EnemyStateMachine stateMachine;
    private Coroutine despawnCheckCoroutine;
    private Camera mainCamera;

    private void Awake()
    {
        stateMachine = GetComponent<EnemyStateMachine>();

        // Set viewportPadding to EnemyData's OffscreenDespawnPadding
        if (stateMachine.Data != null)
        {
            viewportPadding = stateMachine.Data.OffscreenDespawnPadding;
        }
    }

    private void OnEnable()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        despawnCheckCoroutine = StartCoroutine(DespawnCheckRoutine());
    }

    private void OnDisable()
    {
        if (despawnCheckCoroutine != null)
        {
            StopCoroutine(despawnCheckCoroutine);
            despawnCheckCoroutine = null;
        }
    }

    private IEnumerator DespawnCheckRoutine()
    {
        WaitForSeconds wait = new(checkInterval);

        while (true)
        {
            yield return wait;

            if (stateMachine != null && stateMachine.Data != null && !stateMachine.Data.AllowOffscreenDespawn)
            {
                continue;
            }

            if (mainCamera == null)
            {
                mainCamera = Camera.main;
                if (mainCamera == null)
                {
                    continue;
                }
            }

            Vector3 viewportPos = mainCamera.WorldToViewportPoint(transform.position);

            bool offscreen = viewportPos.z < 0f
                || viewportPos.x < -viewportPadding
                || viewportPos.x > 1f + viewportPadding
                || viewportPos.y < -viewportPadding
                || viewportPos.y > 1f + viewportPadding;

            if (offscreen)
            {
                // Releasing to pool disables the GameObject, triggering SpawnerTrackedEnemy.OnDisable()
                // to release the enemy from the spawner's tracking list and unregister it from the SpawnerManager.
                stateMachine.ReleaseToPool();
            }
        }
    }
}