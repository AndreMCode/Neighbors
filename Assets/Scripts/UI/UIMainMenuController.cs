using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using UnityEditor;

/// <summary>
/// Controller for the Main Menu UI, managing visibility, button interactions,
/// and navigation between the main menu and options menu.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class UIMainMenuController : MonoBehaviour
{
    [SerializeField] private RunSessionDataSO sessionData;
    private UIDocument _uiDocument;

    // Menu Containers
    private VisualElement _gameTitle;
    private VisualElement _mainMenu;
    private VisualElement _optionsMenu;

    // Buttons
    private Button _playButton;
    private Button _optionsButton;
    private Button _quitButton;

    // Active State Tracking
    private enum MenuState { Main, Options }
    private MenuState _currentState = MenuState.Main;
    private VisualElement _activeContainer;

    private void Awake()
    {
        //UnityEngine.Cursor.lockState = CursorLockMode.None;
        //UnityEngine.Cursor.visible = true;

        _uiDocument = GetComponent<UIDocument>();
        var root = _uiDocument.rootVisualElement;

        // Container queries
        _gameTitle = root.Q<Label>("game-title");
        _mainMenu = root.Q<VisualElement>("main-menu-container");
        _optionsMenu = root.Q<VisualElement>("options-container");

        // Main Menu Buttons
        _playButton = root.Q<Button>("play-button");
        _optionsButton = root.Q<Button>("options-button");
        _quitButton = root.Q<Button>("quit-button");

        // Main Menu Button Bindings
        _playButton.clicked += () => StartGame();
        _optionsButton.clicked += () => SwitchMenu(MenuState.Options);
        _quitButton.clicked += () => ExitGame();

        // Options Menu Button Bindings
        root.Q<Button>("btn-back-options").clicked += () => SwitchMenu(MenuState.Main);

        // Initialize the Main Menu
        SwitchMenu(MenuState.Main);
    }

    private void SwitchMenu(MenuState newState)
    {
        _currentState = newState;

        // Hide everything first
        _gameTitle.style.display = DisplayStyle.Flex; // Title stays unless otherwise
        _mainMenu.style.display = DisplayStyle.None;
        _optionsMenu.style.display = DisplayStyle.None;

        // Clear focus so nothing is highlighted by default
        _uiDocument.rootVisualElement.Focus();

        // Show the target menu
        switch (newState)
        {
            case MenuState.Main:
                _activeContainer = _mainMenu;
                break;
            case MenuState.Options:
                _activeContainer = _optionsMenu;
                break;
        }

        _activeContainer.style.display = DisplayStyle.Flex;
    }

    private void StartGame()
    {
        if (sessionData != null)
        {
            sessionData.InitializeRunData();
        }

        SceneManager.LoadScene(1);
    }

    private void ExitGame()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}