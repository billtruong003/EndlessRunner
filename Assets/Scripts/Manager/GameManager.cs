using UnityEngine;
using System;
using System.Collections;

public enum GameState
{
    Menu,
    Playing,
    Paused,
    GameOver
}

public class GameManager : Singleton<GameManager>
{
    [Header("Managers")]
    public CoinManager CoinManager;

    [Header("Game Settings")]
    [SerializeField] private int baseMultiplier = 1;
    [SerializeField] private float scorePerSecond = 10f;
    [SerializeField] private float difficultyIncreaseRate = 0.1f;

    [Header("References")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerStat playerStat;

    // Game State
    private GameState currentState = GameState.Menu;
    private float gameTime = 0f;
    private float currentScore = 0f;
    private float highScore = 0f;
    private float distanceTraveled = 0f;

    // Properties
    public int Multiplier { get; private set; }
    public GameState CurrentState => currentState;
    public float GameTime => gameTime;
    public float Score => currentScore;
    public float HighScore => highScore;
    public float Distance => distanceTraveled;

    // Events
    public event Action<GameState> OnGameStateChanged;
    public event Action<float> OnScoreChanged;
    public event Action OnGameStart;
    public event Action OnGameOver;

    protected override void Awake()
    {
        base.Awake();
        LoadHighScore();
        Multiplier = baseMultiplier;

    }

    private void Start()
    {
        if (CoinManager == null)
            CoinManager = GetComponent<CoinManager>();

        // Subscribe to player death event
        if (playerStat != null)
        {
            playerStat.OnPlayerDeath += HandlePlayerDeath;
        }

    }

    private void Update()
    {
        if (currentState == GameState.Playing)
        {
            UpdateGameplay();
        }

        // Debug keys
        if (Input.GetKeyDown(KeyCode.R) && currentState == GameState.GameOver)
        {
            RestartGame();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }

    private void UpdateGameplay()
    {
        // Update game time
        gameTime += Time.deltaTime;

        // Update score based on time and distance
        float scoreThisFrame = scorePerSecond * Time.deltaTime * Multiplier;
        currentScore += scoreThisFrame;
        OnScoreChanged?.Invoke(currentScore);

        // Update distance
        if (playerController != null)
        {
            distanceTraveled += playerController.CurrentForwardSpeed * Time.deltaTime;
        }

        // Increase difficulty over time
        float difficultyMultiplier = 1f + (gameTime * difficultyIncreaseRate);
        // You can use this to increase obstacle spawn rate, speed, etc.
    }

    public void StartGame()
    {
        if (currentState != GameState.Menu) return;

        currentState = GameState.Playing;
        gameTime = 0f;
        currentScore = 0f;
        distanceTraveled = 0f;
        Multiplier = baseMultiplier;

        // Reset player stats
        if (playerStat != null)
        {
            playerStat.ResetStats();
        }

        // Reset coin manager
        if (CoinManager != null)
        {
            CoinManager.SessionCoin = 0;
        }

        Time.timeScale = 1f;
        OnGameStateChanged?.Invoke(currentState);
        OnGameStart?.Invoke();
    }

    public void PauseGame()
    {
        if (currentState != GameState.Playing) return;

        currentState = GameState.Paused;
        Time.timeScale = 0f;
        OnGameStateChanged?.Invoke(currentState);
    }

    public void ResumeGame()
    {
        if (currentState != GameState.Paused) return;

        currentState = GameState.Playing;
        Time.timeScale = 1f;
        OnGameStateChanged?.Invoke(currentState);
    }

    public void TogglePause()
    {
        if (currentState == GameState.Playing)
            PauseGame();
        else if (currentState == GameState.Paused)
            ResumeGame();
    }

    private void HandlePlayerDeath()
    {
        GameOver();
    }

    public void GameOver()
    {
        if (currentState == GameState.GameOver) return;

        currentState = GameState.GameOver;
        Time.timeScale = 0f;

        // Update high score
        if (currentScore > highScore)
        {
            highScore = currentScore;
            SaveHighScore();
        }

        // Add session coins to total
        if (CoinManager != null)
        {
            CoinManager.AddTotalCoin();
        }

        OnGameStateChanged?.Invoke(currentState);
        OnGameOver?.Invoke();
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        // Reload the current scene
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex
        );
    }

    public void BackToMenu()
    {
        currentState = GameState.Menu;
        Time.timeScale = 1f;
        OnGameStateChanged?.Invoke(currentState);
        // Load menu scene if you have one
        // UnityEngine.SceneManagement.SceneManager.LoadScene("MenuScene");
    }

    public void SetMultiplier(int newMultiplier)
    {
        Multiplier = newMultiplier;
    }

    public void AddScore(float points)
    {
        currentScore += points * Multiplier;
        OnScoreChanged?.Invoke(currentScore);
    }

    // Save/Load
    private void SaveHighScore()
    {
        PlayerPrefs.SetFloat("HighScore", highScore);
        PlayerPrefs.Save();
    }

    private void LoadHighScore()
    {
        highScore = PlayerPrefs.GetFloat("HighScore", 0f);
    }

    // Utility methods for other systems
    public float GetDifficultyMultiplier()
    {
        return 1f + (gameTime * difficultyIncreaseRate);
    }

    public bool IsPlaying()
    {
        return currentState == GameState.Playing;
    }
}