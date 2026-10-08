using UnityEngine;

/// <summary>
/// Singleton class to manage the player's spawn point in the game.
/// </summary>
public class PlayerSpawnPoint : MonoBehaviour
{
    public static PlayerSpawnPoint Instance { get; private set; }

    public Vector2 Position => transform.position;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
}