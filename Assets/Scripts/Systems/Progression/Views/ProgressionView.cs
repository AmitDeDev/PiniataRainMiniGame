using UnityEngine;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;

public class ProgressionView : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private Slider xpBar;
    [SerializeField] private TextMeshProUGUI xpBarText;

    [Header("Visual Tweens")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float fillTweenDuration = 0.5f;
    [SerializeField] private float fadeDuration = 0.25f;

    private void Start()
    {
        UpdateUI(PlayerProgression.Instance.CurrentXP, PlayerProgression.Instance.XPToNextLevel);
        UpdateLevel(PlayerProgression.Instance.CurrentLevel);

        PlayerProgression.Instance.OnXPChanged += UpdateUI;
        PlayerProgression.Instance.OnLevelChanged += UpdateLevel;
    }

    private void UpdateUI(int currentXP, int requiredXP)
    {
        if (xpBar != null)
        {
            xpBar.maxValue = requiredXP;
            xpBar.DOValue(currentXP, fillTweenDuration).SetEase(Ease.OutSine);
        }

        if (xpBarText != null)
        {
            xpBarText.text = $"{currentXP} / {requiredXP}";
        }
    }

    private void UpdateLevel(int newLevel)
    {
        if (levelText != null)
        {
            levelText.text = $"Level {newLevel}";
        }
    }

    public void SetVisible(bool isVisible)
    {
        if (!canvasGroup)
        {
            gameObject.SetActive(isVisible);
            return;
        }

        if (isVisible)
        {
            gameObject.SetActive(true);
            canvasGroup.alpha = 0f;
            canvasGroup.DOFade(1f, fadeDuration);
        }
        else
        {
            canvasGroup.DOFade(0f, fadeDuration)
                .OnComplete(() => gameObject.SetActive(false));
        }
    }

    private void OnDestroy()
    {
        if (PlayerProgression.Instance == null) return;

        PlayerProgression.Instance.OnXPChanged -= UpdateUI;
        PlayerProgression.Instance.OnLevelChanged -= UpdateLevel;
    }
}
