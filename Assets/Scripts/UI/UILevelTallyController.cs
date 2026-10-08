using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Controller for the Level Tally UI overlay, managing the display of rescued victims,
/// bonus points, and total score after level completion.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class UILevelTallyController : MonoBehaviour
{
    [Header("Scoring Tuning")]
    [SerializeField] private int pointsPerVictim = 500;
    [SerializeField] private float revealDelay = 1.0f;

    [SerializeField] private RunSessionDataSO sessionData;

    private VisualElement root;
    private Label victimsLabel;
    private Label bonusLabel;
    private Label totalScoreLabel;
    private Button continueButton;

    private void OnEnable()
    {
        // Subscribe to global state changes
        GameStateManager.OnGameStateChanged += HandleGameStateChanged;

        var uiDocument = GetComponent<UIDocument>();
        root = uiDocument.rootVisualElement.Q("tally-root");

        victimsLabel = root.Q<Label>("victims-label");
        bonusLabel = root.Q<Label>("bonus-label");
        totalScoreLabel = root.Q<Label>("total-score-label");
        continueButton = root.Q<Button>("continue-button");

        continueButton.clicked += OnContinueClicked;

        // Hide overlay on awake
        root.style.display = DisplayStyle.None;
    }

    private void OnDisable()
    {
        GameStateManager.OnGameStateChanged -= HandleGameStateChanged;
        continueButton.clicked -= OnContinueClicked;
    }

    private void HandleGameStateChanged(GameState state)
    {
        if (state == GameState.LevelComplete)
        {
            StartCoroutine(TallyRoutine());
        }
        else
        {
            root.style.display = DisplayStyle.None;
        }
    }

    private IEnumerator TallyRoutine()
    {
        // Show UI, hide continue button initially
        root.style.display = DisplayStyle.Flex;
        continueButton.style.display = DisplayStyle.None;

        // Fetch current stats from GameStateManager
        int rescued = GameStateManager.Instance.VictimsRescued;
        int currentScore = sessionData.CurrentScore;
        int bonus = rescued * pointsPerVictim;

        victimsLabel.text = $"VICTIMS RESCUED: {rescued}";
        bonusLabel.text = "BONUS: 0";
        totalScoreLabel.text = $"TOTAL SCORE: {currentScore:D6}";

        // Pause for dramatic effect
        yield return new WaitForSeconds(revealDelay);

        // Award bonus points directly to session data
        if (bonus > 0)
        {
            if (sessionData != null)
            {
                sessionData.AddScore(bonus);
            }
            else if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.AddScore(bonus);
            }

            bonusLabel.text = $"BONUS: {bonus}";
        }

        // Fetch the newly updated score
        int updatedScore = (sessionData != null) ? sessionData.CurrentScore : currentScore + bonus;
        totalScoreLabel.text = $"TOTAL SCORE: {updatedScore:D6}";

        yield return new WaitForSeconds(revealDelay / 2f);

        // Reveal continue button
        continueButton.style.display = DisplayStyle.Flex;
        continueButton.Focus(); // Allows gamepad/keyboard confirmation
    }

    private void OnContinueClicked()
    {
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.LoadNextLevel();
        }
    }
}