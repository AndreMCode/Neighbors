using UnityEngine;

public enum ItemEffectType
{
    None,
    FullHeal,
    SpeedBoost,
    Decoy,
}

/// <summary>
/// Represents the data for an item in the game, including its name, icon, quantity limits, and effect type.
/// </summary>
[CreateAssetMenu(fileName = "ItemData_", menuName = "Engine/Item Data")]
public class ItemData : ScriptableObject
{
    [SerializeField] private string itemName = "First Aid Kit";
    [SerializeField] private Sprite itemIcon;
    [SerializeField] private int maxQuantity = 9;
    [SerializeField] private int startQuantity = 1;
    [SerializeField] private bool consumeOnUse = true;

    [Header("Effect Routing")]
    public ItemEffectType EffectType = ItemEffectType.None;

    [Header("Speed Boost Parameters")]
    [Tooltip("Multiplier applied to base speed (e.g., 1.5 for 50% faster)")]
    public float SpeedMultiplier = 1.5f;
    [Tooltip("Duration of the speed boost in seconds")]
    public float EffectDuration = 20f;

    [Header("Decoy Parameters")]
    [Tooltip("Prefab to instantiate when deploying a decoy.")]
    public GameObject DecoyPrefab;

    public string ItemName => itemName;
    public Sprite ItemIcon => itemIcon;
    public int MaxQuantity => maxQuantity;
    public int StartQuantity => startQuantity;
    public bool ConsumeOnUse => consumeOnUse;
}