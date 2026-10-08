using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Controller for the Game Over UI overlay, managing visibility and interactions based on game state changes.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class UIGameOverController : MonoBehaviour
{
    [SerializeField] private RunSessionDataSO sessionData;
    private VisualElement root;
    private Label finalScoreLabel;
    private Button continueButton;
    private Button quitButton;

    private void OnEnable()
    {
        // Hook into the global state manager
        GameStateManager.OnGameStateChanged += HandleGameStateChanged;

        var uiDocument = GetComponent<UIDocument>();
        root = uiDocument.rootVisualElement.Q("game-over-root");

        finalScoreLabel = root.Q<Label>("final-score-label");
        continueButton = root.Q<Button>("continue-button");
        quitButton = root.Q<Button>("quit-button");

        continueButton.clicked += OnContinueClicked;
        quitButton.clicked += OnQuitClicked;

        // Hide overlay on awake
        root.style.display = DisplayStyle.None;
    }

    private void OnDisable()
    {
        GameStateManager.OnGameStateChanged -= HandleGameStateChanged;

        continueButton.clicked -= OnContinueClicked;
        quitButton.clicked -= OnQuitClicked;
    }

    private void HandleGameStateChanged(GameState state)
    {
        if (state == GameState.GameOver)
        {
            // Fetch final score before showing the UI
            if (sessionData != null)
            {
                finalScoreLabel.text = $"FINAL SCORE: {sessionData.CurrentScore:D6}";
            }

            root.style.display = DisplayStyle.Flex;
            continueButton.Focus(); // Snap UI navigation to the top button
        }
        else
        {
            root.style.display = DisplayStyle.None;
        }
    }

    private void OnContinueClicked()
    {
        // Revert time scale and state before reloading
        GameStateManager.Instance.ChangeState(GameState.Playing);

        GameStateManager.Instance.ContinueAfterGameOver();

        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.RestartCurrentLevel();
        }
    }

    private void OnQuitClicked()
    {
        GameStateManager.Instance.ChangeState(GameState.Playing);

        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.ReturnToMainMenu();
        }
    }
}