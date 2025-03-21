using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    public static MainMenuManager Instance { get; private set; }

    [Header("Managers / Views / Audio")]
    [SerializeField] private MainMenuView mainMenuView;
    [SerializeField] private SceneLoader sceneLoader;
    [SerializeField] private AudioManager audioManager;

    // Popups
    [SerializeField] private GameObject gameRulesPopup;
    [SerializeField] private GameObject bestScorePopup;

    // Best score fields inside bestScorePopup
    [SerializeField] private TextMeshProUGUI bestScoreText;
    [SerializeField] private TextMeshProUGUI bestDestroyedText;
    [SerializeField] private TextMeshProUGUI bestBombUsedText;
    [SerializeField] private TextMeshProUGUI bestCriticalUsedText;
    

    private bool isGameRulePopupActive = false;
    private bool isBestScorePopupActive = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        mainMenuView.Init(this);
        if (gameRulesPopup) gameRulesPopup.SetActive(false);
        if (bestScorePopup) bestScorePopup.SetActive(false);
    }
    
    public void OnGameRulesButtonClicked()
    {
        if (!gameRulesPopup) return;
        
        audioManager?.PlayPiniataClickSound();
        gameRulesPopup.SetActive(true);
        isGameRulePopupActive = true;
    }
    
    public void OnBestScoreButtonClicked()
    {
        if (!bestScorePopup) return;
        
        audioManager?.PlayPiniataClickSound();
        // fetch best stats from PlayerPrefs 
        int bestScore = PlayerPrefs.GetInt("BestScore", 0);
        int bestDestroyed = PlayerPrefs.GetInt("BestDestroyed", 0);
        int bestBombUsed = PlayerPrefs.GetInt("BestBombUsed", 0);
        int bestCritUsed = PlayerPrefs.GetInt("BestCritUsed", 0);

        // fill text fields
        if (bestScoreText) bestScoreText.text = $"Best Score: {bestScore}";
        if (bestDestroyedText) bestDestroyedText.text = $"Highest Piniata Destroyed Amount: {bestDestroyed}";
        if (bestBombUsedText) bestBombUsedText.text = $"Highest Bombs Used: {bestBombUsed}";
        if (bestCriticalUsedText) bestCriticalUsedText.text = $"Highest Critical HIT's Used: {bestCritUsed}";

        bestScorePopup.SetActive(true);
        isBestScorePopupActive = true;
    }
    
    public void OnPopupExitButtonClicked()
    {
        audioManager?.PlayPiniataClickSound();
        if (isGameRulePopupActive && gameRulesPopup)
        {
            gameRulesPopup.SetActive(false);
            isGameRulePopupActive = false;
        }
        else if (isBestScorePopupActive && bestScorePopup)
        {
            bestScorePopup.SetActive(false);
            isBestScorePopupActive = false;
        }
    }
    
    public void OnPlayGameClicked()
    {
        audioManager?.PlayPiniataClickSound();
        sceneLoader.LoadNextScene(); 
    }
    
    public static void TryUpdateBestStats(int finalScore, int finalDestroyed, int bombUsed, int critUsed)
    {
        int storedScore = PlayerPrefs.GetInt("BestScore", 0);
        if (finalScore > storedScore) PlayerPrefs.SetInt("BestScore", finalScore);

        int storedDestroyed = PlayerPrefs.GetInt("BestDestroyed", 0);
        if (finalDestroyed > storedDestroyed) PlayerPrefs.SetInt("BestDestroyed", finalDestroyed);

        int storedBombUsed = PlayerPrefs.GetInt("BestBombUsed", 0);
        if (bombUsed > storedBombUsed) PlayerPrefs.SetInt("BestBombUsed", bombUsed);

        int storedCritUsed = PlayerPrefs.GetInt("BestCritUsed", 0);
        if (critUsed > storedCritUsed) PlayerPrefs.SetInt("BestCritUsed", critUsed);

        PlayerPrefs.Save();
    }
}
