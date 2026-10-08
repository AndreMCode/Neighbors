using UnityEngine;

/// <summary>
/// Manages the player's inventory, including weapons and utility items, and handles firing projectiles and using items.
/// </summary>
public class PlayerInventory : MonoBehaviour
{
    [Tooltip("Offsets the origin of fired projectiles")]
    [SerializeField] private Vector2 _originOffset = new(0f, 0.4f);

    [Header("Input Reference")]
    [SerializeField] private InputReader inputReader;

    [Header("Session Data")]
    [SerializeField] private RunSessionDataSO sessionData;

    private HealthComponent healthComponent;
    private PlayerStateMachine playerStateMachine;
    private float nextFireTime;

    private void Awake()
    {
        healthComponent = GetComponent<HealthComponent>();
        playerStateMachine = GetComponent<PlayerStateMachine>();

        if (inputReader == null && playerStateMachine != null)
        {
            inputReader = playerStateMachine.InputReader;
        }
    }

    private void OnEnable()
    {
        if (inputReader != null)
        {
            inputReader.OnUseItemPerformed += HandleUseItem;
        }
    }

    private void OnDisable()
    {
        if (inputReader != null)
        {
            inputReader.OnUseItemPerformed -= HandleUseItem;
        }
    }

    private void Start()
    {
        if (sessionData != null)
        {
            sessionData.BroadcastCurrentState();
        }
    }

    public bool TryFire(Vector2 origin, Vector2 direction)
    {
        if (sessionData == null) return false;

        WeaponData currentWeapon = sessionData.GetCurrentWeapon();
        if (currentWeapon == null || Time.time < nextFireTime) return false;

        if (!sessionData.AmmoTable.TryGetValue(currentWeapon, out int currentAmmo) || currentAmmo <= 0)
            return false;

        sessionData.ModifyAmmo(currentWeapon, -1);

        // Set fire rate cooldown timer
        nextFireTime = Time.time + currentWeapon.FireRate;

        Vector2 newOrigin = origin + _originOffset;

        // Spawn projectile from pool
        GameObject projObj = ProjectilePoolManager.Instance.SpawnProjectile(
            currentWeapon.ProjectilePrefab,
            newOrigin,
            Quaternion.identity
        );

        if (projObj != null && projObj.TryGetComponent(out Projectile projectile))
        {
            projectile.Initialize(currentWeapon, direction);
        }

        return true;
    }

    private void HandleUseItem()
    {
        if (sessionData == null) return;

        if (sessionData.TryConsumeCurrentItem(out ItemData item))
        {
            ExecuteItemEffect(item);
        }
    }

    private void ExecuteItemEffect(ItemData item)
    {
        if (item == null) return;
        Debug.Log($"[PlayerInventory] Executing utility item: {item.ItemName}");

        switch (item.EffectType)
        {
            case ItemEffectType.FullHeal:
                if (healthComponent != null)
                {
                    healthComponent.Heal(healthComponent.MaxHealth);
                }
                break;

            case ItemEffectType.SpeedBoost:
                if (playerStateMachine != null)
                {
                    playerStateMachine.ApplySpeedBoost(item.SpeedMultiplier, item.EffectDuration);
                }
                break;

            case ItemEffectType.Decoy:
                if (item.DecoyPrefab != null)
                {
                    Instantiate(item.DecoyPrefab, transform.position, Quaternion.identity);
                }
                else
                {
                    Debug.LogWarning($"[PlayerInventory] No DecoyPrefab assigned on {item.ItemName}!");
                }
                break;

            case ItemEffectType.None:
            default:
                Debug.LogWarning($"[PlayerInventory] Item {item.ItemName} has no EffectType assigned!");
                break;
        }
    }
}