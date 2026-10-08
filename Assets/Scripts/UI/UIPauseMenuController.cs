using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Controller for the Pause Menu UI overlay, managing visibility and interactions based on game state changes.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class UIPauseMenuController : MonoBehaviour
{
    private VisualElement root;
    private Button resumeButton;
    private Button optionsButton;
    private Button quitButton;

    private void OnEnable()
    {
        // Hook into the global state manager
        GameStateManager.OnGameStateChanged += HandleGameStateChanged;

        var uiDocument = GetComponent<UIDocument>();
        root = uiDocument.rootVisualElement.Q("pause-root");

        resumeButton = root.Q<Button>("resume-button");
        optionsButton = root.Q<Button>("options-button");
        quitButton = root.Q<Button>("quit-button");

        // Bind button actions
        resumeButton.clicked += OnResumeClicked;
        optionsButton.clicked += OnOptionsClicked;
        quitButton.clicked += OnQuitClicked;

        // Hide overlay on awake
        root.style.display = DisplayStyle.None;
    }

    private void OnDisable()
    {
        GameStateManager.OnGameStateChanged -= HandleGameStateChanged;

        resumeButton.clicked -= OnResumeClicked;
        optionsButton.clicked -= OnOptionsClicked;
        quitButton.clicked -= OnQuitClicked;
    }

    private void HandleGameStateChanged(GameState state)
    {
        if (state == GameState.Paused)
        {
            root.style.display = DisplayStyle.Flex;
            resumeButton.Focus(); // Snap UI navigation to the top button
        }
        else
        {
            root.style.display = DisplayStyle.None;
        }
    }

    private void OnResumeClicked()
    {
        // Revert back to gameplay
        GameStateManager.Instance.ChangeState(GameState.Playing);
    }

    private void OnOptionsClicked()
    {
        // Placeholder for future Audio/Video settings menu overlay
        Debug.Log("Options menu triggered (Not yet implemented).");
    }

    private void OnQuitClicked()
    {
        // Ensure time is unpaused before returning to the main menu
        GameStateManager.Instance.ChangeState(GameState.Playing);

        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.ReturnToMainMenu();
        }
    }
}