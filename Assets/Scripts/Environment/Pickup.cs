using UnityEngine;

/// <summary>
/// Represents a pickup item in the game that can be collected by the player.
/// When the player collides with this pickup,it will apply its effect (e.g., adding keys, weapons, or items)
/// to the player's session data and optionally play a collection visual effect before destroying itself.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Pickup : MonoBehaviour
{
    [SerializeField] private RunSessionDataSO runSessionData;
    [SerializeField] private PickupData pickupData;
    [SerializeField] private GameObject collectVfxPrefab;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        Collect();
    }

    private void Collect()
    {
        switch (pickupData.PickupType)
        {
            case PickupType.Key:
                runSessionData.AddKey(pickupData.IsSkullKey);
                break;

            case PickupType.Weapon:
                runSessionData.AddWeaponOrAmmo(pickupData.Weapon, pickupData.AmmoAmount);
                break;

            case PickupType.Item:
                runSessionData.AddItemOrQuantity(pickupData.Item, pickupData.ItemAmount);
                break;
        }

        if (collectVfxPrefab != null)
        {
            Instantiate(collectVfxPrefab, transform.position, Quaternion.identity);
        }

        Destroy(gameObject);
    }
}