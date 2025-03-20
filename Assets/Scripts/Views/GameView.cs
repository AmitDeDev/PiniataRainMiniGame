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

    [Header("GameObjects and UI")]
    [SerializeField] private Button bombButton;
    [SerializeField] private Button criticalButton;
    [SerializeField] private GameObject Notifications;

    // If you have leftover references, you can remove them:0
    // [SerializeField] private GameObject piniataOnCooldownObj;

    private void Awake()
    {
        // ...
    }

    public void Init(GameManager manager)
    {
        manager.OnScoreUpdated += UpdateScore;
        manager.OnBombCountUpdated += UpdateBombUI;
        manager.OnCriticalCountUpdated += UpdateCriticalUI;

        bombButton.AddSquishEffect(0.8f,0.1f);
        bombButton.onClick.AddListener(() => manager.OnBombButtonClicked());

        criticalButton.AddSquishEffect(0.8f, 0.1f);
        criticalButton.onClick.AddListener(() => manager.OnCriticalButtonClicked());

        Notifications.gameObject.SetActive(false);
    }

    public void UpdateScore(int newScore)
    {
        if (scoreText) scoreText.text = $"Score: {newScore}";
    }

    public void UpdateTimer(float timeLeft)
    {
        if (timerText) timerText.text = "Time Left: " + Mathf.Ceil(timeLeft);
    }

    public void UpdatePiniatasDestroyed(int val)
    {
        if (destroyedCountText) destroyedCountText.text = $"Destroyed: {val}";
    }

    public void UpdateBombUI(int newBombCount)
    {
        if (bombCountText)
        {
            bombCountText.text = $"x{newBombCount}";
            bombButton.interactable = (newBombCount > 0);
        }
    }

    public void UpdateCriticalUI(int newCriticalCount)
    {
        if (criticalCountText)
        {
            criticalCountText.text = $"x{newCriticalCount}";
            criticalButton.interactable = (newCriticalCount > 0);
        }
    }

    public void ShowNotificationsWithTimer(float duration, string text)
    {
        StartCoroutine(Notification(duration,text));
    }

    private IEnumerator Notification(float duration, string text)
    {
        if (Notifications != null)
        {
            Notifications.SetActive(true);
            TextMeshProUGUI notifText = Notifications.GetComponentInChildren<TextMeshProUGUI>();
            if (notifText != null) notifText.text = text;

            yield return new WaitForSeconds(duration);

            Notifications.SetActive(false);
        }
    }
}
