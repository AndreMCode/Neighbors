using UnityEngine;

public enum WeaponDeliveryType
{
    DirectLinear,
    LobbedAoE
}

/// <summary>
/// ScriptableObject that holds configuration data for a weapon,
/// including ammo, firing rate, projectile properties, and delivery type.
/// </summary>
[CreateAssetMenu(fileName = "WeaponData_", menuName = "Engine/Weapon Data")]
public class WeaponData : ScriptableObject
{
    [Header("General Configuration")]
    [SerializeField] private string weaponName = "Water Gun";
    [SerializeField] private WeaponDeliveryType deliveryType = WeaponDeliveryType.DirectLinear;
    [SerializeField] private Sprite weaponIcon;

    [Header("Ammo & Firing Loop")]
    [SerializeField] private int maxAmmo = 100;
    [SerializeField] private int startAmmo = 10;
    [SerializeField] private float fireRate = 0.15f; // Time delay between shots (in seconds)
    [SerializeField] private bool allowsMovementWhileFiring = true; // Run-and-gun flag

    [Header("Projectile Properties")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private GameObject impactVfxPrefab;
    [SerializeField] private float damage = 10f;
    [SerializeField] private float projectileSpeed = 12f;
    [SerializeField] private float maxRange = 5f; // Travel limit before recycling
    [SerializeField] private float knockbackForce = 1.5f;

    [Header("Lobbed / AoE Parameters (For Arc Delivery)")]
    [SerializeField] private float lobDuration = 0.6f; // Time in seconds the projectile remains in air
    [SerializeField] private float arcPeakHeight = 1.5f; // Visual height peak (spoofed Y-axis offset)
    [SerializeField] private float aoeRadius = 2.5f;     // Damage radius triggered on landing

    // Public Read-Only Properties
    public string WeaponName => weaponName;
    public WeaponDeliveryType DeliveryType => deliveryType;
    public Sprite WeaponIcon => weaponIcon;
    public int MaxAmmo => maxAmmo;
    public int StartAmmo => startAmmo;
    public float FireRate => fireRate;
    public bool AllowsMovementWhileFiring => allowsMovementWhileFiring;
    public GameObject ProjectilePrefab => projectilePrefab;
    public GameObject ImpactVFXPrefab => impactVfxPrefab;
    public float Damage => damage;
    public float ProjectileSpeed => projectileSpeed;
    public float MaxRange => maxRange;
    public float KnockbackForce => knockbackForce;
    public float LobDuration => lobDuration;
    public float ArcPeakHeight => arcPeakHeight;
    public float AoeRadius => aoeRadius;
}