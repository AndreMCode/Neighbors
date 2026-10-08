using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class LockedDoor : MonoBehaviour
{
    [Header("Door Configuration")]
    [SerializeField] private bool isSkullDoor = false;
    [SerializeField] private Sprite openSprite;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private RunSessionDataSO sessionData;

    [Header("Optional Feedback")]
    [SerializeField] private GameObject unlockVFX;

    private Collider2D doorCollider;
    private bool isOpen = false;

    private void Awake()
    {
        doorCollider = GetComponent<Collider2D>();
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isOpen) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            TryUnlock();
        }
    }

    private void OnTriggerEnter2D(Collider2D collider)
    {
        if (isOpen) return;

        if (collider.CompareTag("Player"))
        {
            TryUnlock();
        }
    }

    private void TryUnlock()
    {
        bool unlocked = false;
        if (sessionData != null)
        {
            unlocked = sessionData.TryConsumeKey(isSkullDoor);
        }
        else if (GameStateManager.Instance != null)
        {
            unlocked = GameStateManager.Instance.TryConsumeKey(isSkullDoor);
        }

        if (unlocked)
        {
            OpenDoor();
        }
        else
        {
            // Player bumped into door without required key
            Debug.Log($"[LockedDoor] Door is locked. Requires a {(isSkullDoor ? "Skull Key" : "Regular Key")}.");
        }
    }

    private void OpenDoor()
    {
        isOpen = true;

        // Disable physical collision barrier so player can walk through
        if (doorCollider != null)
        {
            doorCollider.enabled = false;
        }

        // Update visuals to open door sprite (or hide GameObject if no sprite provided)
        if (spriteRenderer != null && openSprite != null)
        {
            spriteRenderer.sprite = openSprite;
        }
        else
        {
            gameObject.SetActive(false);
        }

        // Spawn optional particle effect
        if (unlockVFX != null)
        {
            Instantiate(unlockVFX, transform.position, Quaternion.identity);
        }

        Debug.Log($"[LockedDoor] {(isSkullDoor ? "Skull Gate" : "Standard Door")} unlocked!");
    }
}