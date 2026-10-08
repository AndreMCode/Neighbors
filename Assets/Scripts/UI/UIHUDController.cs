using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Controller for the in-game HUD (Heads-Up Display), managing the
/// display of player stats, health, cooldowns, and inventory items.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class UIHUDController : MonoBehaviour
{
    [SerializeField] private RunSessionDataSO sessionData;

    private Label scoreLabel;
    private Label livesLabel;
    private Label keysLabel;

    private VisualElement healthBarFill;
    private VisualElement dashCooldownFill;

    private VisualElement weaponIcon;
    private Label weaponAmmoLabel;

    private VisualElement itemIcon;
    private Label itemCountLabel;

    private void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        scoreLabel = root.Q<Label>("score-label");
        livesLabel = root.Q<Label>("lives-label");
        keysLabel = root.Q<Label>("keys-label");

        healthBarFill = root.Q<VisualElement>("health-bar-fill");
        dashCooldownFill = root.Q<VisualElement>("dash-cooldown-fill");

        weaponIcon = root.Q<VisualElement>("weapon-icon");
        weaponAmmoLabel = root.Q<Label>("weapon-ammo-label");

        itemIcon = root.Q<VisualElement>("item-icon");
        itemCountLabel = root.Q<Label>("item-count-label");

        // Subscribe to Session Data Events
        if (sessionData != null)
        {
            sessionData.OnScoreChanged += UpdateScore;
            sessionData.OnLivesChanged += UpdateLives;
            sessionData.OnKeysChanged += UpdateKeys;
            sessionData.OnWeaponSwapped += UpdateWeaponIcon;
            sessionData.OnAmmoChanged += UpdateWeaponAmmo;
            sessionData.OnItemSwapped += UpdateItemIcon;
            sessionData.OnItemQuantityChanged += UpdateItemCount;
        }

        GameStateManager.OnPlayerHealthChanged += UpdateHealthBar;
    }

    private void Start()
    {
        if (sessionData != null)
        {
            sessionData.BroadcastCurrentState();
        }
    }

    private void Update()
    {
        UpdateDashCooldownBar();
    }

    private void OnDisable()
    {
        if (sessionData != null)
        {
            sessionData.OnScoreChanged -= UpdateScore;
            sessionData.OnLivesChanged -= UpdateLives;
            sessionData.OnKeysChanged -= UpdateKeys;
            sessionData.OnWeaponSwapped -= UpdateWeaponIcon;
            sessionData.OnAmmoChanged -= UpdateWeaponAmmo;
            sessionData.OnItemSwapped -= UpdateItemIcon;
            sessionData.OnItemQuantityChanged -= UpdateItemCount;
        }

        GameStateManager.OnPlayerHealthChanged -= UpdateHealthBar;
    }

    private void UpdateScore(int score) => scoreLabel.text = $"SCORE: {score:D6}";
    private void UpdateLives(int lives) => livesLabel.text = $"LIVES: {Mathf.Max(0, lives)}";
    private void UpdateKeys(int regular, int skulls) => keysLabel.text = $"KEYS: {regular}  S.KEYS: {skulls}";

    private void UpdateHealthBar(float currentHealth, float maxHealth)
    {
        if (healthBarFill == null) return;

        float percentage = Mathf.Clamp01(currentHealth / maxHealth) * 100f;
        healthBarFill.style.width = Length.Percent(percentage);

        if (percentage > 50f) healthBarFill.style.backgroundColor = Color.green;
        else if (percentage > 25f) healthBarFill.style.backgroundColor = Color.yellow;
        else healthBarFill.style.backgroundColor = Color.red;
    }

    private void UpdateDashCooldownBar()
    {
        if (dashCooldownFill == null) return;

        var player = GameStateManager.Instance != null ? GameStateManager.Instance.CurrentPlayer : null;
        if (player == null) return;

        float percentage = player.DashReadiness01 * 100f;
        dashCooldownFill.style.width = Length.Percent(percentage);
    }

    private void UpdateWeaponIcon(WeaponData weapon)
    {
        if (weapon != null && weapon.WeaponIcon != null)
        {
            weaponIcon.style.backgroundImage = new StyleBackground(weapon.WeaponIcon);
        }
        else
        {
            weaponIcon.style.backgroundImage = null;
        }
    }

    private void UpdateWeaponAmmo(WeaponData weapon, int ammo)
    {
        weaponAmmoLabel.text = ammo.ToString();
    }

    private void UpdateItemIcon(ItemData item)
    {
        if (item != null && item.ItemIcon != null)
        {
            itemIcon.style.backgroundImage = new StyleBackground(item.ItemIcon);
        }
        else
        {
            itemIcon.style.backgroundImage = null;
        }
    }

    private void UpdateItemCount(ItemData item, int count)
    {
        itemCountLabel.text = count.ToString();
    }
}