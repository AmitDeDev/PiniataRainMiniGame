using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuView : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button playGameButton;
    [SerializeField] private Button gameRulesButton;
    [SerializeField] private Button bestScoreButton;
    [SerializeField] private TextMeshProUGUI coinBalanceText;
    
    private void Start()
    {
        if (BalanceController.Instance != null)
        {
            BalanceController.Instance.OnBalanceChanged += UpdateBalanceUI;
            UpdateBalanceUI(BalanceController.Instance.GetBalance());
        }
    }

    public void Init(MainMenuManager manager)
    {
        if (playGameButton)
        {
            playGameButton.AddSquishEffect(0.8f, 0.1f);
            playGameButton.onClick.AddListener(() => manager.OnPlayGameClicked());
        }
        
        if (gameRulesButton)
        {
            gameRulesButton.AddSquishEffect(0.8f, 0.1f);
            gameRulesButton.onClick.AddListener(() => manager.OnGameRulesButtonClicked());
        }
        
        if (bestScoreButton)
        {
            bestScoreButton.AddSquishEffect(0.8f, 0.1f);
            bestScoreButton.onClick.AddListener(() => manager.OnBestScoreButtonClicked());
        }
    }
    
    private void UpdateBalanceUI(int balance)
    {
        if (coinBalanceText != null)
            coinBalanceText.text = $"{balance}";
    }
}