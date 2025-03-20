using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    public static MainMenuManager Instance { get; private set; }
    
    [Header("Managers / Views")]
    [SerializeField] private MainMenuView mainMenuView;
    [SerializeField] private AudioManager audioManager;
    
    [Header("Popup GameObject")] 
    [SerializeField] private GameObject gameRulesPopup;
    [SerializeField] private GameObject bestScorePopup;


    private bool isGameRulePopupActive;
    private bool isBestScorePopupActive;
    
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        mainMenuView.Init(this);
    }

    public void OnGameRulesButtonClicked()
    {
        isGameRulePopupActive = true;
        audioManager.PlayPiniataClickSound();
        gameRulesPopup.gameObject.SetActive(true);
    }
    
    public void OnBestScoreButtonClicked()
    {
        isBestScorePopupActive = true;
        audioManager.PlayPiniataClickSound();
        bestScorePopup.gameObject.SetActive(true);
    }

    public void OnPopupExitButtonClicked()
    {
        audioManager.PlayPiniataClickSound();
        if (gameRulesPopup.gameObject == isGameRulePopupActive)
        {
            gameRulesPopup.SetActive(false);
            isGameRulePopupActive = false;
        }
        else if (bestScorePopup.gameObject == isBestScorePopupActive)
        {
            bestScorePopup.SetActive(false);
            isBestScorePopupActive = false;
        }
    }
    
}
