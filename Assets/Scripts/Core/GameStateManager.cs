using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }

    [Header("Input")]
    [SerializeField] private InputReader inputReader;

    [Header("Respawn Settings")]
    [SerializeField] private float respawnDelay = 1.5f;

    [SerializeField] private RunSessionDataSO sessionData;
    public RunSessionDataSO SessionData => sessionData;
    private PlayerStateMachine playerRef;
    public PlayerStateMachine CurrentPlayer => playerRef;

    // Active Victim Tracking
    private readonly List<VictimComponent> activeVictims = new();
    private readonly HashSet<VictimComponent> collectedVictims = new();
    public IReadOnlyList<VictimComponent> ActiveVictims => activeVictims;

    // Victim Statistics
    public int TotalVictimsInLevel { get; private set; }
    public int VictimsCollected { get; private set; }
    public int VictimsRescued { get; private set; }
    public int VictimsKilled { get; private set; }

    // Decoy Tracking
    private readonly List<Decoy> activeDecoys = new();
    public IReadOnlyList<Decoy> ActiveDecoys => activeDecoys;

    public GameState CurrentState { get; private set; }

    // Events
    public static event Action<GameState> OnGameStateChanged;
    public static event Action<float, float> OnPlayerHealthChanged; // (currentHealth, maxHealth)
    public static event Action<int, int> OnVictimsUpdated; // (rescued, totalInLevel)
    public static event Action<int> OnActiveVictimCountChanged; // (remainingCount)
    public static event Action OnExitPortalUnlocked;

    private void Awake()
    {
        // Ensure singleton pattern: only one instance of GameStateManager should exist.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        CurrentState = GameState.Playing;
    }

    private void OnEnable()
    {
        PlayerDeathState.OnPlayerDied += HandlePlayerDied;

        if (inputReader != null)
        {
            inputReader.OnTogglePausePerformed += TogglePause;
        }
    }

    private void OnDisable()
    {
        PlayerDeathState.OnPlayerDied -= HandlePlayerDied;

        if (inputReader != null)
        {
            inputReader.OnTogglePausePerformed -= TogglePause;
        }
    }

    private void Start()
    {
        OnGameStateChanged?.Invoke(CurrentState);
    }

    public void ChangeState(GameState newState)
    {
        if (CurrentState == newState) return;

        CurrentState = newState;
        OnGameStateChanged?.Invoke(CurrentState);

        // Enable or disable gameplay input based on the new state.
        bool gameplayInputAllowed = newState != GameState.LevelComplete;
        inputReader.InputEnabled = gameplayInputAllowed;

        Time.timeScale = (CurrentState == GameState.Paused) ? 0f : 1f;
    }

    public void TogglePause()
    {
        if (CurrentState == GameState.Playing)
        {
            ChangeState(GameState.Paused);
        }
        else if (CurrentState == GameState.Paused)
        {
            ChangeState(GameState.Playing);
        }
    }

    // Registers a player with the GameStateManager, subscribing to health change events and initializing health.
    public void RegisterPlayer(PlayerStateMachine player)
    {
        if (playerRef != null && playerRef.Health != null)
        {
            playerRef.Health.OnHealthChanged -= HandlePlayerHealthChanged;
        }

        playerRef = player;

        if (playerRef != null && playerRef.Health != null)
        {
            playerRef.Health.OnHealthChanged += HandlePlayerHealthChanged;

            playerRef.Health.InitializeHealth(sessionData.CurrentHealth, sessionData.MaxHealth);

            OnPlayerHealthChanged?.Invoke(playerRef.Health.CurrentHealth, playerRef.Health.MaxHealth);
        }
    }

    // Unregisters a player from the GameStateManager, unsubscribing from health change events.
    public void UnregisterPlayer(PlayerStateMachine player)
    {
        if (playerRef == player)
        {
            if (playerRef.Health != null)
            {
                playerRef.Health.OnHealthChanged -= HandlePlayerHealthChanged;
            }
            playerRef = null;
        }
    }

    private void HandlePlayerHealthChanged(float currentHealth, float maxHealth)
    {
        sessionData.SetHealth(currentHealth, maxHealth);

        OnPlayerHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void LoseLife()
    {
        if (sessionData != null)
        {
            bool isAlive = sessionData.LoseLife();
            if (!isAlive)
            {
                ChangeState(GameState.GameOver);
            }
            else
            {
                Debug.Log("Life lost. Respawning player...");
            }
        }
    }

    public void AddScore(int points)
    {
        if (sessionData != null)
            sessionData.AddScore(points);
    }

    public bool TryConsumeKey(bool isSkullKey)
    {
        return sessionData != null && sessionData.TryConsumeKey(isSkullKey);
    }

    public void ResetVictimTracker()
    {
        activeVictims.Clear();
        collectedVictims.Clear();
        TotalVictimsInLevel = 0;
        VictimsCollected = 0;
        VictimsRescued = 0;
        VictimsKilled = 0;
        OnActiveVictimCountChanged?.Invoke(0);
        OnVictimsUpdated?.Invoke(0, 0);
    }

    // Registers a victim with the GameStateManager, adding it to the active victims list and updating statistics.
    public void RegisterVictim(VictimComponent victim)
    {
        if (victim == null || activeVictims.Contains(victim)) return;

        activeVictims.Add(victim);
        TotalVictimsInLevel++;
        OnActiveVictimCountChanged?.Invoke(activeVictims.Count);
        OnVictimsUpdated?.Invoke(VictimsRescued, TotalVictimsInLevel);
    }

    // Unregisters a victim from the GameStateManager, removing it from the active victims list and updating statistics.
    public void UnregisterVictim(VictimComponent victim)
    {
        if (victim != null && activeVictims.Remove(victim))
        {
            TotalVictimsInLevel = Mathf.Max(0, TotalVictimsInLevel - 1);
            OnActiveVictimCountChanged?.Invoke(activeVictims.Count);
            OnVictimsUpdated?.Invoke(VictimsRescued, TotalVictimsInLevel);
        }
    }

    public void CollectVictim(VictimComponent victim)
    {
        if (victim != null)
        {
            activeVictims.Remove(victim);
            collectedVictims.Add(victim);
        }

        VictimsCollected++;

        OnActiveVictimCountChanged?.Invoke(activeVictims.Count);
        OnVictimsUpdated?.Invoke(VictimsRescued, TotalVictimsInLevel);

        EvaluateLevelCompletion();
    }

    public void RescueVictim(VictimComponent victim, int scorePoints)
    {
        if (victim != null)
        {
            collectedVictims.Remove(victim);
        }

        VictimsRescued++;

        // UILevelTallyController already awards points,
        // *** TO DO: bonus for rescuing more than one victim ***
        // AddScore(scorePoints);

        OnVictimsUpdated?.Invoke(VictimsRescued, TotalVictimsInLevel);
    }

    public void KillVictim(VictimComponent victim)
    {
        // Check if the victim was in the collected queue and remove it if so
        bool wasQueued = victim != null && collectedVictims.Remove(victim);

        if (victim != null)
        {
            activeVictims.Remove(victim);
        }

        if (wasQueued)
        {
            VictimsCollected = Mathf.Max(0, VictimsCollected - 1);
        }

        VictimsKilled++;

        OnActiveVictimCountChanged?.Invoke(activeVictims.Count);
        OnVictimsUpdated?.Invoke(VictimsRescued, TotalVictimsInLevel);

        EvaluateLevelCompletion();
    }

    // Finds the closest active victim to the specified search position within the given maximum radius.
    public VictimComponent GetClosestVictim(Vector2 searchPosition, float maxRadius = float.MaxValue)
    {
        VictimComponent closest = null;
        float closestSqrDistance = maxRadius * maxRadius;

        for (int i = activeVictims.Count - 1; i >= 0; i--)
        {
            VictimComponent v = activeVictims[i];
            if (v == null || !v.gameObject.activeInHierarchy) continue;

            float sqrDist = ((Vector2)v.transform.position - searchPosition).sqrMagnitude;
            if (sqrDist < closestSqrDistance)
            {
                closestSqrDistance = sqrDist;
                closest = v;
            }
        }

        return closest;
    }

    public void RegisterDecoy(Decoy decoy)
    {
        if (decoy == null || activeDecoys.Contains(decoy)) return;
        activeDecoys.Add(decoy);
    }

    public void UnregisterDecoy(Decoy decoy)
    {
        if (decoy != null)
        {
            activeDecoys.Remove(decoy);
        }
    }

    // Finds the closest active decoy to the specified search position within the given maximum radius.
    public Decoy GetClosestDecoy(Vector2 searchPosition, float maxRadius = float.MaxValue)
    {
        Decoy closest = null;
        float closestSqrDistance = maxRadius * maxRadius;

        for (int i = activeDecoys.Count - 1; i >= 0; i--)
        {
            Decoy d = activeDecoys[i];
            if (d == null || !d.gameObject.activeInHierarchy) continue;

            float sqrDist = ((Vector2)d.transform.position - searchPosition).sqrMagnitude;
            if (sqrDist < closestSqrDistance)
            {
                closestSqrDistance = sqrDist;
                closest = d;
            }
        }

        return closest;
    }

    private void EvaluateLevelCompletion()
    {
        int totalResolved = VictimsCollected + VictimsKilled;

        if (totalResolved >= TotalVictimsInLevel && TotalVictimsInLevel > 0)
        {
            if (VictimsCollected > 0)
            {
                Debug.Log("All victims collected or perished! Exit door unlocked.");
                OnExitPortalUnlocked?.Invoke();
            }
            else
            {
                Debug.Log("All victims perished! Game Over.");
                ChangeState(GameState.GameOver);
            }
        }
    }

    public void ContinueAfterGameOver()
    {
        if (sessionData != null)
        {
            sessionData.ContinueRun();
        }
    }

    private void HandlePlayerDied()
    {
        StartCoroutine(RespawnRoutine());
    }

    // Coroutine to handle player respawn after a delay, losing a life and respawning the player at the spawn point.
    private IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(respawnDelay);

        LoseLife();

        if (CurrentState != GameState.GameOver && playerRef != null)
        {
            Vector2 spawnPos = (PlayerSpawnPoint.Instance != null)
                ? PlayerSpawnPoint.Instance.Position
                : Vector2.zero;

            playerRef.Respawn(spawnPos);
        }
    }
}