using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RunSessionData", menuName = "Game Data/Run Session Data")]
public class RunSessionDataSO : ScriptableObject
{
    [Header("Stats")]
    [SerializeField] private int currentLives;
    public int CurrentLives => currentLives;
    [SerializeField] private int currentScore;
    public int CurrentScore => currentScore;
    [SerializeField] private int regularKeys;
    public int RegularKeys => regularKeys;
    [SerializeField] private int skullKeys;
    public int SkullKeys => skullKeys;
    [SerializeField] private float currentHealth;
    public float CurrentHealth => currentHealth;
    [SerializeField] private float maxHealth;
    public float MaxHealth => maxHealth;

    [Header("Weapon Inventory")]
    [SerializeField] private List<WeaponData> startWeapons = new();
    public IReadOnlyList<WeaponData> StartWeapons => startWeapons;
    [SerializeField] private List<WeaponData> ownedWeapons = new();
    public IReadOnlyList<WeaponData> OwnedWeapons => ownedWeapons;
    [SerializeField] private int currentWeaponIndex = 0;
    public int CurrentWeaponIndex => currentWeaponIndex;
    public WeaponData CurrentWeapon => (ownedWeapons != null && ownedWeapons.Count > 0 && currentWeaponIndex >= 0 && currentWeaponIndex < ownedWeapons.Count)
        ? ownedWeapons[currentWeaponIndex]
        : null;
    public Dictionary<WeaponData, int> AmmoTable { get; } = new Dictionary<WeaponData, int>();

    [Header("Item Inventory")]
    [SerializeField] private List<ItemData> startItems = new();
    public IReadOnlyList<ItemData> StartItems => startItems;
    [SerializeField] private List<ItemData> ownedItems = new();
    public IReadOnlyList<ItemData> OwnedItems => ownedItems;
    [SerializeField] private int currentItemIndex = 0;
    public int CurrentItemIndex => currentItemIndex;
    public ItemData CurrentItem => (ownedItems != null && ownedItems.Count > 0 && currentItemIndex >= 0 && currentItemIndex < ownedItems.Count)
        ? ownedItems[currentItemIndex]
        : null;
    public Dictionary<ItemData, int> ItemQuantities { get; } = new Dictionary<ItemData, int>();

    // Instance Events
    public event Action<int> OnLivesChanged;
    public event Action<int> OnScoreChanged;
    public event Action<int, int> OnKeysChanged;
    public event Action<WeaponData, int> OnAmmoChanged;
    public event Action<WeaponData> OnWeaponSwapped;
    public event Action<ItemData, int> OnItemQuantityChanged;
    public event Action<ItemData> OnItemSwapped;
    public event Action<float, float> OnHealthChanged;

    private readonly int startingLives = 2;
    private readonly int startingScore = 0;
    private readonly float startingHealth = 100f;

    public void InitializeRunData()
    {
        currentLives = startingLives;
        currentScore = startingScore;
        regularKeys = 0;
        skullKeys = 0;
        currentHealth = startingHealth;
        maxHealth = startingHealth;

        ownedWeapons.Clear();
        if (startWeapons != null)
        {
            ownedWeapons.AddRange(startWeapons);
        }

        AmmoTable.Clear();
        currentWeaponIndex = 0;

        foreach (var weapon in ownedWeapons)
        {
            if (weapon != null)
            {
                AmmoTable[weapon] = weapon.StartAmmo;
            }
        }

        ownedItems.Clear();
        if (startItems != null)
        {
            ownedItems.AddRange(startItems);
        }

        ItemQuantities.Clear();
        currentItemIndex = 0;

        foreach (var item in ownedItems)
        {
            if (item != null)
            {
                ItemQuantities[item] = item.StartQuantity;
            }
        }

        BroadcastCurrentState();
    }

    public void ContinueRun()
    {
        currentLives = startingLives;
        currentHealth = startingHealth;
        maxHealth = startingHealth;

        BroadcastCurrentState();
    }

    public void SetHealth(float current, float max)
    {
        currentHealth = current;
        maxHealth = max;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void AddScore(int points)
    {
        currentScore += points;
        OnScoreChanged?.Invoke(currentScore);
    }

    public void AddLife()
    {
        currentLives++;
        OnLivesChanged?.Invoke(currentLives);
    }

    public bool LoseLife()
    {
        currentLives--;
        OnLivesChanged?.Invoke(currentLives);
        return currentLives >= 0;
    }

    public void AddKey(bool isSkullKey)
    {
        if (isSkullKey) skullKeys++;
        else regularKeys++;

        OnKeysChanged?.Invoke(regularKeys, skullKeys);
    }

    public bool TryConsumeKey(bool isSkullKey)
    {
        if (isSkullKey && skullKeys > 0)
        {
            skullKeys--;
            OnKeysChanged?.Invoke(regularKeys, skullKeys);
            return true;
        }
        else if (!isSkullKey && regularKeys > 0)
        {
            regularKeys--;
            OnKeysChanged?.Invoke(regularKeys, skullKeys);
            return true;
        }

        return false;
    }

    public void AddWeaponOrAmmo(WeaponData weapon, int ammoAmount)
    {
        if (weapon == null) return;

        if (!ownedWeapons.Contains(weapon))
        {
            ownedWeapons.Add(weapon);
            AmmoTable[weapon] = 0;
        }

        ModifyAmmo(weapon, ammoAmount);
    }

    public void ModifyAmmo(WeaponData weapon, int amount)
    {
        if (weapon == null) return;

        if (!AmmoTable.ContainsKey(weapon))
        {
            AmmoTable[weapon] = 0;
        }

        AmmoTable[weapon] = Mathf.Clamp(AmmoTable[weapon] + amount, 0, weapon.MaxAmmo);
        OnAmmoChanged?.Invoke(weapon, AmmoTable[weapon]);
    }

    public WeaponData GetCurrentWeapon() => CurrentWeapon;

    public int GetAmmo(WeaponData weapon)
    {
        if (weapon == null) return 0;
        return AmmoTable.TryGetValue(weapon, out int ammo) ? ammo : 0;
    }

    public void EquipWeaponAtIndex(int index)
    {
        if (index < 0 || index >= ownedWeapons.Count) return;
        if (ownedWeapons[index] == null) return;

        currentWeaponIndex = index;

        WeaponData selectedWeapon = CurrentWeapon;
        OnWeaponSwapped?.Invoke(selectedWeapon);

        if (selectedWeapon != null && AmmoTable.TryGetValue(selectedWeapon, out int ammo))
        {
            OnAmmoChanged?.Invoke(selectedWeapon, ammo);
        }
    }

    public ItemData GetCurrentItem() => CurrentItem;

    public int GetItemQuantity(ItemData item)
    {
        if (item == null) return 0;
        return ItemQuantities.TryGetValue(item, out int qty) ? qty : 0;
    }

    public void EquipItemAtIndex(int index)
    {
        if (index < 0 || index >= ownedItems.Count) return;
        if (ownedItems[index] == null) return;

        currentItemIndex = index;

        ItemData selectedItem = CurrentItem;
        OnItemSwapped?.Invoke(selectedItem);

        if (selectedItem != null)
        {
            OnItemQuantityChanged?.Invoke(selectedItem, GetItemQuantity(selectedItem));
        }
    }

    public void AddItemOrQuantity(ItemData item, int amount)
    {
        if (item == null) return;

        if (!ownedItems.Contains(item))
        {
            ownedItems.Add(item);
            ItemQuantities[item] = 0;
        }

        ItemQuantities[item] = Mathf.Clamp(ItemQuantities[item] + amount, 0, item.MaxQuantity);

        if (item == CurrentItem)
        {
            OnItemQuantityChanged?.Invoke(CurrentItem, ItemQuantities[CurrentItem]);
        }
    }

    public bool CanConsumeCurrentItem()
    {
        return CurrentItem != null && GetItemQuantity(CurrentItem) > 0;
    }

    public bool TryConsumeCurrentItem(out ItemData itemToExecute)
    {
        itemToExecute = CurrentItem;
        if (itemToExecute == null) return false;

        int count = GetItemQuantity(itemToExecute);
        if (count <= 0) return false;

        if (itemToExecute.ConsumeOnUse)
        {
            ItemQuantities[itemToExecute] = count - 1;
            OnItemQuantityChanged?.Invoke(itemToExecute, ItemQuantities[itemToExecute]);
        }

        return true;
    }

    public void BroadcastCurrentState()
    {
        OnLivesChanged?.Invoke(currentLives);
        OnScoreChanged?.Invoke(currentScore);
        OnKeysChanged?.Invoke(regularKeys, skullKeys);

        WeaponData current = CurrentWeapon;
        if (current != null)
        {
            OnWeaponSwapped?.Invoke(current);
            if (AmmoTable.TryGetValue(current, out int ammo))
            {
                OnAmmoChanged?.Invoke(current, ammo);
            }
        }

        ItemData currentItm = CurrentItem;
        if (currentItm != null)
        {
            OnItemSwapped?.Invoke(currentItm);
            OnItemQuantityChanged?.Invoke(currentItm, GetItemQuantity(currentItm));
        }
    }
}