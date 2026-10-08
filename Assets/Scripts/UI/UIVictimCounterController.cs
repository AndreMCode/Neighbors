using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Controller for the Victim Counter UI overlay, managing the display of the
/// current count of active victims in the game.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class UIVictimCounterController : MonoBehaviour
{
    [Header("UI Document References")]
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private string countLabelName = "victim-count-label";

    private Label victimCountLabel;

    private void OnEnable()
    {
        if (uiDocument == null) uiDocument = GetComponent<UIDocument>();

        VisualElement root = uiDocument.rootVisualElement;
        victimCountLabel = root.Q<Label>(countLabelName);

        // Subscribe to VictimManager count updates
        GameStateManager.OnActiveVictimCountChanged += UpdateDisplay;

        // Initialize display if VictimManager is already active
        if (GameStateManager.Instance != null)
        {
            UpdateDisplay(GameStateManager.Instance.ActiveVictims.Count);
        }
    }

    private void OnDisable()
    {
        GameStateManager.OnActiveVictimCountChanged -= UpdateDisplay;
    }

    private void UpdateDisplay(int count)
    {
        if (victimCountLabel == null) return;

        // Formats to two digits
        victimCountLabel.text = count.ToString("D2");
    }
}