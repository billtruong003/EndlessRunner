using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class UIManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private GameObject gameplayPanel;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject gameOverPanel;

    [Header("Gameplay UI")]
    [SerializeField] private Text scoreText;
    [SerializeField] private Text coinText;
    [SerializeField] private Text distanceText;
    [SerializeField] private Text multiplierText;
    [SerializeField] private Slider healthBar;
    [SerializeField] private GameObject shieldIcon;

    [Header("Game Over UI")]
    [SerializeField] private Text finalScoreText;
    [SerializeField] private Text highScoreText;
    [SerializeField] private Text coinsEarnedText;
    [SerializeField] private Text distanceTraveledText;

    [Header("Menu UI")]
    [SerializeField] private Text totalCoinsText;
    [SerializeField] private Text bestScoreText;

    private GameManager gameManager;
    private PlayerStat playerStat;
    private CoinManager coinManager;

    private void Start()
    {
        gameManager = GameManager.Instance;
        coinManager = gameManager.CoinManager;

        // Find player stat
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerStat = player.GetComponent<PlayerStat>();
            if (playerStat != null)
            {
                playerStat.OnHealthChanged += UpdateHealthBar;
                playerStat.OnShieldStateChanged += UpdateShieldIcon;
            }
        }

        // Subscribe to game events
        gameManager.OnGameStateChanged += OnGameStateChanged;
        gameManager.OnScoreChanged += UpdateScore;

        // Initialize UI
        ShowPanel(GameState.Menu);
        UpdateMenuUI();
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (gameManager != null)
        {
            gameManager.OnGameStateChanged -= OnGameStateChanged;
            gameManager.OnScoreChanged -= UpdateScore;
        }

        if (playerStat != null)
        {
            playerStat.OnHealthChanged -= UpdateHealthBar;
            playerStat.OnShieldStateChanged -= UpdateShieldIcon;
        }
    }

    private void Update()
    {
        if (gameManager.IsPlaying())
        {
            UpdateGameplayUI();
        }
    }

    private void OnGameStateChanged(GameState newState)
    {
        ShowPanel(newState);

        switch (newState)
        {
            case GameState.Menu:
                UpdateMenuUI();
                break;
            case GameState.Playing:
                ResetGameplayUI();
                break;
            case GameState.GameOver:
                ShowGameOverStats();
                break;
        }
    }

    private void ShowPanel(GameState state)
    {
        // Hide all panels
        if (menuPanel != null) menuPanel.SetActive(false);
        if (gameplayPanel != null) gameplayPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        // Show the appropriate panel
        switch (state)
        {
            case GameState.Menu:
                if (menuPanel != null) menuPanel.SetActive(true);
                break;
            case GameState.Playing:
                if (gameplayPanel != null) gameplayPanel.SetActive(true);
                break;
            case GameState.Paused:
                if (pausePanel != null) pausePanel.SetActive(true);
                if (gameplayPanel != null) gameplayPanel.SetActive(true);
                break;
            case GameState.GameOver:
                if (gameOverPanel != null) gameOverPanel.SetActive(true);
                break;
        }
    }

    private void UpdateGameplayUI()
    {
        // Update coins
        if (coinText != null && coinManager != null)
        {
            coinText.text = coinManager.SessionCoin.ToString();
        }

        // Update distance
        if (distanceText != null)
        {
            distanceText.text = $"{Mathf.FloorToInt(gameManager.Distance)}m";
        }

        // Update multiplier
        if (multiplierText != null)
        {
            if (gameManager.Multiplier > 1)
            {
                multiplierText.text = $"x{gameManager.Multiplier}";
                multiplierText.gameObject.SetActive(true);
            }
            else
            {
                multiplierText.gameObject.SetActive(false);
            }
        }
    }

    private void UpdateScore(float score)
    {
        if (scoreText != null)
        {
            scoreText.text = Mathf.FloorToInt(score).ToString();
        }
    }

    private void UpdateHealthBar(int currentHealth)
    {
        if (healthBar != null && playerStat != null)
        {
            healthBar.value = playerStat.GetHealthPercentage();
        }
    }

    private void UpdateShieldIcon(bool isActive)
    {
        if (shieldIcon != null)
        {
            shieldIcon.SetActive(isActive);
        }
    }

    private void ResetGameplayUI()
    {
        UpdateScore(0);
        UpdateHealthBar(playerStat != null ? playerStat.GetCurrentHealth() : 100);
        UpdateShieldIcon(false);
    }

    private void UpdateMenuUI()
    {
        if (totalCoinsText != null && coinManager != null)
        {
            totalCoinsText.text = $"Coins: {coinManager.GetTotalCoin()}";
        }

        if (bestScoreText != null)
        {
            bestScoreText.text = $"Best: {Mathf.FloorToInt(gameManager.HighScore)}";
        }
    }

    private void ShowGameOverStats()
    {
        if (finalScoreText != null)
        {
            finalScoreText.text = $"Score: {Mathf.FloorToInt(gameManager.Score)}";
        }

        if (highScoreText != null)
        {
            highScoreText.text = $"Best: {Mathf.FloorToInt(gameManager.HighScore)}";
        }

        if (coinsEarnedText != null && coinManager != null)
        {
            coinsEarnedText.text = $"Coins: +{coinManager.SessionCoin}";
        }

        if (distanceTraveledText != null)
        {
            distanceTraveledText.text = $"Distance: {Mathf.FloorToInt(gameManager.Distance)}m";
        }
    }

    // Button callbacks
    public void OnPlayButtonClicked()
    {
        gameManager.StartGame();
    }

    public void OnPauseButtonClicked()
    {
        gameManager.PauseGame();
    }

    public void OnResumeButtonClicked()
    {
        gameManager.ResumeGame();
    }

    public void OnRestartButtonClicked()
    {
        gameManager.RestartGame();
    }

    public void OnMainMenuButtonClicked()
    {
        gameManager.BackToMenu();
    }

    public void OnQuitButtonClicked()
    {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}