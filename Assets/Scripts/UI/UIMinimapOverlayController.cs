using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Controller for the Minimap UI overlay, managing visibility based on player input.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class UIMinimapOverlayController : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private InputReader inputReader;

    [Header("UI Element Names")]
    [SerializeField] private string minimapContainerName = "radar-frame";

    private VisualElement minimapContainer;
    private bool isMinimapVisible = false;

    private void OnEnable()
    {
        if (uiDocument == null) uiDocument = GetComponent<UIDocument>();

        VisualElement root = uiDocument.rootVisualElement;
        minimapContainer = root.Q<VisualElement>(minimapContainerName);

        if (inputReader != null)
        {
            inputReader.OnToggleMinimapPerformed += HandleToggleMinimap;
        }

        SetMinimapVisibility(false);
    }

    private void OnDisable()
    {
        if (inputReader != null)
        {
            inputReader.OnToggleMinimapPerformed -= HandleToggleMinimap;
        }
    }

    private void HandleToggleMinimap()
    {
        isMinimapVisible = !isMinimapVisible;
        SetMinimapVisibility(isMinimapVisible);
    }

    private void SetMinimapVisibility(bool visible)
    {
        if (minimapContainer == null) return;

        minimapContainer.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }
}