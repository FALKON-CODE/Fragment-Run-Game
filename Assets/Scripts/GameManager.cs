using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Central game-flow controller for a level: tracks the current state,
/// handles winning (reaching the exit), losing (falling), pausing and scene
/// loading. One instance lives per scene and is reachable via <see cref="Instance"/>.
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
    public float restartDelay = 0.8f;

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

        // Make sure time is running (it may have been paused before a scene load).
        Time.timeScale = 1f;

        // Every gameplay scene gets its UI built alongside the GameManager.
        if (UIManager.Instance == null)
            gameObject.AddComponent<UIManager>();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) &&
            (State == GameState.Playing || State == GameState.Paused))
        {
            TogglePause();
        }
    }

    // ----- State transitions --------------------------------------------

    public void TogglePause()
    {
        if (State == GameState.Playing)
        {
            State = GameState.Paused;
            Time.timeScale = 0f;
            if (UIManager.Instance != null) UIManager.Instance.SetPauseVisible(true);
        }
        else if (State == GameState.Paused)
        {
            ResumeGame();
        }
    }

    public void ResumeGame()
    {
        State = GameState.Playing;
        Time.timeScale = 1f;
        if (UIManager.Instance != null) UIManager.Instance.SetPauseVisible(false);
    }

    /// <summary>Called when the player enters the exit trigger.</summary>
    public void LevelComplete()
    {
        if (State != GameState.Playing) return;

        State = GameState.LevelComplete;
        if (UIManager.Instance != null) UIManager.Instance.ShowBanner("LEVEL COMPLETE");
        Invoke(nameof(LoadNextLevel), levelCompleteDelay);
    }

    /// <summary>Called when the player falls out of the level or hits a hazard.</summary>
    public void PlayerDied()
    {
        if (State != GameState.Playing) return;

        State = GameState.GameOver;
        if (UIManager.Instance != null) UIManager.Instance.ShowBanner("YOU DIED");
        Invoke(nameof(RestartLevel), restartDelay);
    }

    void LoadNextLevel()
    {
        string nextScene = "Level" + (CurrentLevel + 1);

        if (Application.CanStreamedLevelBeLoaded(nextScene))
            SceneManager.LoadScene(nextScene);
        else if (UIManager.Instance != null)
            UIManager.Instance.ShowWin(); // Finished the last level.
        else
            RestartLevel();
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }
}
