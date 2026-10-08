using UnityEngine;

/// <summary>
/// Defines the types of pickups available in the game, such as keys, weapons, and items.
/// </summary>
public enum PickupType
{
    Key,
    Weapon,
    Item,
}

[CreateAssetMenu(fileName = "PickupData_", menuName = "Engine/Pickup Data")]
public class PickupData : ScriptableObject
{
    [SerializeField] private PickupType pickupType;

    [Header("Key Pickup")]
    [SerializeField] private bool isSkullKey;

    [Header("Weapon Pickup")]
    [SerializeField] private WeaponData weapon;
    [SerializeField] private int ammoAmount;

    [Header("Item Pickup")]
    [SerializeField] private ItemData item;
    [SerializeField] private int itemAmount;

    public PickupType PickupType => pickupType;
    public bool IsSkullKey => isSkullKey;
    public WeaponData Weapon => weapon;
    public int AmmoAmount => ammoAmount;
    public ItemData Item => item;
    public int ItemAmount => itemAmount;
}