using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class GameView : MonoBehaviour
{
    [Header("Texts")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI destroyedCountText;
    [SerializeField] private TextMeshProUGUI bombCountText;
    [SerializeField] private TextMeshProUGUI criticalCountText;

    [Header("Popups")]
    [SerializeField] private GameObject pausePopup;
    [SerializeField] private GameObject gameOverPopup;

    [Header("GameOver Popup Fields")]
    [SerializeField] private TextMeshProUGUI finalScoreText;
    [SerializeField] private TextMeshProUGUI finalDestroyedText;
    [SerializeField] private TextMeshProUGUI finalBombUsedText;
    [SerializeField] private TextMeshProUGUI finalCriticalUsedText;

    [Header("Buttons")]
    [SerializeField] private Button bombButton;
    [SerializeField] private Button criticalButton;
    [SerializeField] private Button pauseButton;

    [Header("Notifications")]
    [SerializeField] private GameObject Notifications;

    private GameManager gameManager;

    public void Init(GameManager manager)
    {
        gameManager = manager;

        manager.OnScoreUpdated += UpdateScore;
        manager.OnBombCountUpdated += UpdateBombUI;
        manager.OnCriticalCountUpdated += UpdateCriticalUI;

        // BOMB
        if (bombButton)
        {
            bombButton.AddSquishEffect(0.8f,0.1f);
            bombButton.onClick.AddListener(() => manager.OnBombButtonClicked());
        }

        // CRITICAL
        if (criticalButton)
        {
            criticalButton.AddSquishEffect(0.8f, 0.1f);
            criticalButton.onClick.AddListener(() => manager.OnCriticalButtonClicked());
        }

        // PAUSE
        if (pauseButton)
        {
            pauseButton.AddSquishEffect(0.8f, 0.1f);
            pauseButton.onClick.AddListener(() => manager.OnPauseButtonClicked());
        }

        // Hide popups
        if (Notifications) Notifications.SetActive(false);
        if (pausePopup) pausePopup.SetActive(false);
        if (gameOverPopup) gameOverPopup.SetActive(false);
    }

    #region UI Updates
    
    public void UpdateScore(int newScore)
    {
        if (scoreText) 
            scoreText.text = $"Coins: {newScore}";
    }

    public void UpdateTimer(float timeLeft)
    {
        if (timerText) 
            timerText.text = "Time Left: " + Mathf.Ceil(timeLeft);
    }

    public void UpdatePiniatasDestroyed(int val)
    {
        if (destroyedCountText) 
            destroyedCountText.text = $"Destroyed: {val}";
    }

    public void UpdateBombUI(int newBombCount)
    {
        if (bombCountText)
        {
            bombCountText.text = $"x{newBombCount}";
            if (bombButton) bombButton.interactable = (newBombCount > 0);
        }
    }

    public void UpdateCriticalUI(int newCriticalCount)
    {
        if (criticalCountText)
        {
            criticalCountText.text = $"x{newCriticalCount}";
            if (criticalButton) criticalButton.interactable = (newCriticalCount > 0);
        }
    }
    
    #endregion

    # region Popups

    public void ShowPausePopup(bool show)
    {
        if (pausePopup) 
            pausePopup.SetActive(show);
    }
    
    public void ShowGameOverPopup(int finalScore, int finalDestroyed, int finalBombUsed, int finalCritUsed)
    {
        if (gameOverPopup)
        {
            gameOverPopup.SetActive(true);
            if (finalScoreText)       finalScoreText.text       = $"Coins Collected:  {finalScore}";
            if (finalDestroyedText)   finalDestroyedText.text   = $"Piniatas Destroyed: {finalDestroyed}";
            if (finalBombUsedText)    finalBombUsedText.text    = $"Bomb Used: {finalBombUsed}";
            if (finalCriticalUsedText)finalCriticalUsedText.text= $"Critical HIT Used: {finalCritUsed}";
        }
    }
    
    #endregion

    #region Popup Buttons

    
    public void OnContinueButtonClicked()
    {
        gameManager?.ResumeGame();
    }

    public void OnPauseMenuBackClicked()
    {
        gameManager?.BackToMainMenuFromPause();
    }

    // Game Over Popup
    public void OnGameOverPlayAgainClicked()
    {
        gameManager?.RestartGame();
    }

    public void OnGameOverMainMenuClicked()
    {
        gameManager?.BackToMainMenuFromGameOver();
    }
    
    #endregion
    

    #region Notifications
    
    public void ShowNotificationsWithTimer(string text, float duration)
    {
        StartCoroutine(Notification(duration, text));
    }

    private IEnumerator Notification(float duration, string text)
    {
        if (Notifications)
        {
            Notifications.SetActive(true);
            TextMeshProUGUI notifText = Notifications.GetComponentInChildren<TextMeshProUGUI>();
            if (notifText != null)
            {
                notifText.text = text;
            }
            yield return new WaitForSecondsRealtime(duration);

            Notifications.SetActive(false);
        }
    }
    
    
    #endregion
}
