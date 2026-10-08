using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

public enum RadialOverlayMode
{
    Closed,
    Weapon,
    Utility
}

/// <summary>
/// Controller for the Radial Overlay UI, managing the display and interaction of weapon and utility selection rings.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class UIRadialOverlayController : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private InputReader inputReader;
    private readonly RadialDirectionUtility radialUtility = new();
    [FormerlySerializedAs("sessionInventory")]
    [SerializeField] private RunSessionDataSO sessionData;
    [SerializeField] private Transform playerTransform;

    [Header("UI Element Names")]
    [SerializeField] private string weaponRingName = "weapon-overlay-ring";
    [SerializeField] private string utilityRingName = "utility-overlay-ring";
    [SerializeField] private string pageIndicatorName = "page-indicator-label";

    private VisualElement weaponRing;
    private VisualElement utilityRing;
    private Label pageIndicator;

    private readonly List<VisualElement> weaponSlots = new();
    private readonly List<VisualElement> utilitySlots = new();

    private RadialOverlayMode currentMode = RadialOverlayMode.Closed;
    private int activePageIndex = 0; // 0 = Page 1 (0-7), 1 = Page 2 (8-15)
    private int highlightedSlotIndex = -1;
    private bool hasSelectionBeenMade = false;

    private void OnEnable()
    {
        if (uiDocument == null) uiDocument = GetComponent<UIDocument>();

        VisualElement root = uiDocument.rootVisualElement;
        weaponRing = root.Q<VisualElement>(weaponRingName);
        utilityRing = root.Q<VisualElement>(utilityRingName);
        pageIndicator = root.Q<Label>(pageIndicatorName);

        // Populate 8 slots for both Weapon and Utility rings
        weaponSlots.Clear();
        utilitySlots.Clear();
        for (int i = 0; i < 8; i++)
        {
            VisualElement wSlot = root.Q<VisualElement>($"slot-{i}");
            if (wSlot != null) weaponSlots.Add(wSlot);

            VisualElement uSlot = root.Q<VisualElement>($"utility-slot-{i}");
            if (uSlot != null) utilitySlots.Add(uSlot);
        }

        inputReader.OnToggleWeaponOverlayPerformed += HandleToggleWeaponOverlay;
        inputReader.OnToggleUtilityOverlayPerformed += HandleToggleUtilityOverlay;
        inputReader.OnConfirmOverlayPerformed += HandleConfirm;
        inputReader.OnCancelOverlayPerformed += HandleCancel;

        CloseAllOverlays();
    }

    private void OnDisable()
    {
        inputReader.OnToggleWeaponOverlayPerformed -= HandleToggleWeaponOverlay;
        inputReader.OnToggleUtilityOverlayPerformed -= HandleToggleUtilityOverlay;
        inputReader.OnConfirmOverlayPerformed -= HandleConfirm;
        inputReader.OnCancelOverlayPerformed -= HandleCancel;
    }

    private void Update()
    {
        if (currentMode == RadialOverlayMode.Closed) return;

        UpdateRingScreenPosition();
        EvaluateStickyRightStick();
    }

    private void HandleToggleWeaponOverlay()
    {
        if (currentMode == RadialOverlayMode.Weapon)
        {
            // Tapping LT while Weapon overlay is open flips pages
            activePageIndex = (activePageIndex == 0) ? 1 : 0;
            RefreshWeaponSlots();
        }
        else
        {
            // Open Weapon mode (automatically overrides and closes Utility mode)
            OpenMode(RadialOverlayMode.Weapon);
        }
    }

    private void HandleToggleUtilityOverlay()
    {
        if (currentMode == RadialOverlayMode.Utility)
        {
            // Tapping RT while Utility overlay is open toggles it closed
            CloseAllOverlays();
        }
        else
        {
            // Open Utility mode (automatically overrides and closes Weapon mode)
            OpenMode(RadialOverlayMode.Utility);
        }
    }

    private void OpenMode(RadialOverlayMode mode)
    {
        currentMode = mode;
        activePageIndex = 0;
        highlightedSlotIndex = -1;
        hasSelectionBeenMade = false;

        if (weaponRing != null) weaponRing.style.display = (mode == RadialOverlayMode.Weapon) ? DisplayStyle.Flex : DisplayStyle.None;
        if (utilityRing != null) utilityRing.style.display = (mode == RadialOverlayMode.Utility) ? DisplayStyle.Flex : DisplayStyle.None;

        if (mode == RadialOverlayMode.Weapon) RefreshWeaponSlots();
        else if (mode == RadialOverlayMode.Utility) RefreshUtilitySlots();
    }

    public void CloseAllOverlays()
    {
        currentMode = RadialOverlayMode.Closed;
        highlightedSlotIndex = -1;
        hasSelectionBeenMade = false;

        if (weaponRing != null) weaponRing.style.display = DisplayStyle.None;
        if (utilityRing != null) utilityRing.style.display = DisplayStyle.None;

        ClearSlotHighlights(weaponSlots);
        ClearSlotHighlights(utilitySlots);
    }

    private void EvaluateStickyRightStick()
    {
        int detectedOctant = radialUtility.GetOctantIndex(inputReader.RightStickValue);

        if (detectedOctant != -1)
        {
            highlightedSlotIndex = detectedOctant;
            hasSelectionBeenMade = true;

            List<VisualElement> activeSlots = (currentMode == RadialOverlayMode.Weapon) ? weaponSlots : utilitySlots;
            ApplySlotHighlight(activeSlots, highlightedSlotIndex);
        }
    }

    private void HandleConfirm()
    {
        if (currentMode == RadialOverlayMode.Closed) return;

        // Abort if no stick selection was made
        if (!hasSelectionBeenMade || highlightedSlotIndex == -1)
        {
            CloseAllOverlays();
            return;
        }

        // Commit inventory swap based on active mode
        if (sessionData != null)
        {
            if (currentMode == RadialOverlayMode.Weapon)
            {
                int targetIndex = (activePageIndex * 8) + highlightedSlotIndex;
                sessionData.EquipWeaponAtIndex(targetIndex);
            }
            else if (currentMode == RadialOverlayMode.Utility)
            {
                sessionData.EquipItemAtIndex(highlightedSlotIndex);
            }
        }

        CloseAllOverlays();
    }

    private void HandleCancel()
    {
        if (currentMode == RadialOverlayMode.Closed) return;
        CloseAllOverlays();
    }

    private void UpdateRingScreenPosition()
    {
        if (playerTransform == null || Camera.main == null) return;

        VisualElement activeRing = (currentMode == RadialOverlayMode.Weapon) ? weaponRing : utilityRing;
        if (activeRing == null) return;

        Vector3 screenPos = Camera.main.WorldToScreenPoint(playerTransform.position);
        float uiX = screenPos.x;
        float uiY = Screen.height - screenPos.y;

        activeRing.style.left = uiX - (activeRing.resolvedStyle.width / 2f);
        activeRing.style.top = uiY - (activeRing.resolvedStyle.height / 2f);
    }

    private void ApplySlotHighlight(List<VisualElement> slots, int activeIndex)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if (i == activeIndex) slots[i].AddToClassList("slot-highlighted");
            else slots[i].RemoveFromClassList("slot-highlighted");
        }
    }

    private void ClearSlotHighlights(List<VisualElement> slots)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            slots[i].RemoveFromClassList("slot-highlighted");
        }
    }

    private void RefreshWeaponSlots()
    {
        if (pageIndicator != null) pageIndicator.text = $"PAGE {activePageIndex + 1}";
        if (sessionData == null) return;

        IReadOnlyList<WeaponData> weapons = sessionData.OwnedWeapons;
        int startIndex = activePageIndex * 8;

        for (int i = 0; i < 8; i++)
        {
            int invIndex = startIndex + i;
            if (i < weaponSlots.Count)
            {
                if (invIndex < weapons.Count && weapons[invIndex] != null)
                {
                    weaponSlots[i].style.opacity = 1.0f;
                    weaponSlots[i].style.backgroundImage = new StyleBackground(weapons[invIndex].WeaponIcon);
                }
                else
                {
                    weaponSlots[i].style.opacity = 0.2f;
                    weaponSlots[i].style.backgroundImage = null;
                }
            }
        }
    }

    private void RefreshUtilitySlots()
    {
        if (sessionData == null) return;

        IReadOnlyList<ItemData> items = sessionData.OwnedItems;

        for (int i = 0; i < 8; i++)
        {
            if (i < utilitySlots.Count)
            {
                if (i < items.Count && items[i] != null)
                {
                    utilitySlots[i].style.opacity = 1.0f;
                    utilitySlots[i].style.backgroundImage = new StyleBackground(items[i].ItemIcon);
                }
                else
                {
                    utilitySlots[i].style.opacity = 0.2f;
                    utilitySlots[i].style.backgroundImage = null;
                }
            }
        }
    }
}