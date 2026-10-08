using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    public static event Action OnLevelLoadStarted; // Event triggered when a level load starts
    public static event Action OnLevelLoadCompleted; // Event triggered when a level load completes

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        // Persistence is handled if attached to the same GameObject as GameStateManager
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    public void LoadNextLevel()
    {
        int nextBuildIndex = SceneManager.GetActiveScene().buildIndex + 1;

        if (nextBuildIndex < SceneManager.sceneCountInBuildSettings)
        {
            StartCoroutine(LoadSceneAsync(nextBuildIndex));
        }
        else
        {
            Debug.Log("Final level completed. Returning to Main Menu.");
            ReturnToMainMenu();
        }
    }

    public void RestartCurrentLevel()
    {
        StartCoroutine(LoadSceneAsync(SceneManager.GetActiveScene().buildIndex));
    }

    public void ReturnToMainMenu()
    {
        StartCoroutine(LoadSceneAsync(0));
    }

    private IEnumerator LoadSceneAsync(int buildIndex)
    {
        OnLevelLoadStarted?.Invoke();

        // Trigger loading screen stuff here (UI fade coroutine, block input, etc.)

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(buildIndex);

        // Timers or progress bars can be updated here if needed

        while (!asyncLoad.isDone)
        {
            yield return null;
        }
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (GameStateManager.Instance != null)
        {
            // Clear out stale victims from the previous level
            GameStateManager.Instance.ResetVictimTracker();

            // Resume normal gameplay state
            GameStateManager.Instance.ChangeState(GameState.Playing);
        }

        OnLevelLoadCompleted?.Invoke();
    }
}