using System;
using UnityEngine;
using UnityEngine.InputSystem;

[CreateAssetMenu(fileName = "InputReader", menuName = "Engine/Input Reader")]
public class InputReader : ScriptableObject, PlayerControls.IGameplayActions
{
    public Vector2 MovementValue { get; private set; }
    public Vector2 RightStickValue { get; private set; }
    public bool IsFiring { get; private set; }
    public bool InputEnabled { get; set; } = true;

    public event Action OnDashPerformed;
    public event Action OnUseItemPerformed;
    public event Action OnToggleWeaponOverlayPerformed;
    public event Action OnToggleUtilityOverlayPerformed;
    public event Action OnConfirmOverlayPerformed;
    public event Action OnCancelOverlayPerformed;
    public event Action OnToggleMinimapPerformed;
    public event Action OnTogglePausePerformed;

    private PlayerControls controls;

    private void OnEnable()
    {
        if (controls == null)
        {
            controls = new PlayerControls();
            controls.Gameplay.SetCallbacks(this);
        }
        controls.Gameplay.Enable();
    }

    private void OnDisable()
    {
        controls?.Gameplay.Disable();
    }

    public void OnMove(InputAction.CallbackContext context) => MovementValue = context.ReadValue<Vector2>();

    public void OnRightStick(InputAction.CallbackContext context) => RightStickValue = context.ReadValue<Vector2>();

    public void OnFire(InputAction.CallbackContext context)
    {
        if (!InputEnabled) return;

        if (context.started || context.performed) IsFiring = true;
        else if (context.canceled) IsFiring = false;
    }

    public void OnDash(InputAction.CallbackContext context)
    {
        if (!InputEnabled) return;
        if (context.performed) OnDashPerformed?.Invoke();
    }

    public void OnUseItem(InputAction.CallbackContext context)
    {
        if (!InputEnabled) return;

        if (context.performed) OnUseItemPerformed?.Invoke();
    }

    public void OnToggleWeaponOverlay(InputAction.CallbackContext context)
    {
        if (!InputEnabled) return;

        if (context.performed) OnToggleWeaponOverlayPerformed?.Invoke();
    }

    public void OnToggleUtilityOverlay(InputAction.CallbackContext context)
    {
        if (!InputEnabled) return;

        if (context.performed) OnToggleUtilityOverlayPerformed?.Invoke();
    }

    public void OnConfirmOverlay(InputAction.CallbackContext context)
    {
        if (context.performed) OnConfirmOverlayPerformed?.Invoke();
    }

    public void OnCancelOverlay(InputAction.CallbackContext context)
    {
        if (context.performed) OnCancelOverlayPerformed?.Invoke();
    }

    public void OnToggleMinimap(InputAction.CallbackContext context)
    {
        if (!InputEnabled) return;

        if (context.performed) OnToggleMinimapPerformed?.Invoke();
    }

    public void OnTogglePause(InputAction.CallbackContext context)
    {
        if (context.performed) OnTogglePausePerformed?.Invoke();
    }
}