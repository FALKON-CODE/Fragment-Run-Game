using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Central game-flow controller for a level: tracks the current state,
/// handles winning (reaching the exit), losing (falling) and scene loading.
/// One instance lives per scene and is reachable through <see cref="Instance"/>.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState { Playing, LevelComplete, GameOver, Paused }
    public GameState State { get; private set; } = GameState.Playing;

    /// <summary>Number of the level currently loaded (set by <see cref="LevelManager"/>).</summary>
    public int CurrentLevel { get; set; } = 1;

    [Header("Timing")]
    [Tooltip("Delay before the next level loads after reaching the exit.")]
    public float levelCompleteDelay = 1f;
    [Tooltip("Delay before restarting after the player dies.")]
    public float restartDelay = 0.6f;

    /// <summary>True while the player is allowed to control the character.</summary>
    public bool IsPlaying => State == GameState.Playing;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>Called when the player enters the exit trigger.</summary>
    public void LevelComplete()
    {
        if (State != GameState.Playing) return;

        State = GameState.LevelComplete;
        Invoke(nameof(LoadNextLevel), levelCompleteDelay);
    }

    /// <summary>Called when the player falls out of the level or hits a hazard.</summary>
    public void PlayerDied()
    {
        if (State != GameState.Playing) return;

        State = GameState.GameOver;
        Invoke(nameof(RestartLevel), restartDelay);
    }

    void LoadNextLevel()
    {
        string nextScene = "Level" + (CurrentLevel + 1);

        if (Application.CanStreamedLevelBeLoaded(nextScene))
            SceneManager.LoadScene(nextScene);
        else
            RestartLevel(); // Last level for now; a proper win screen arrives with the UI.
    }

    public void RestartLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
