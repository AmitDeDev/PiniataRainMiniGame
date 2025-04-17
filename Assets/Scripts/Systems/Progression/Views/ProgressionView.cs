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

    [Header("XP Gain Feedback")]
    [SerializeField] private TextMeshProUGUI xpGainText;
    [SerializeField] private RectTransform xpGainRect;
    [SerializeField] private RectTransform xpGainStartReference;
    [SerializeField] private float gainTextDuration = 2f;
    [SerializeField] private float gainTextOffset = 50f;

    private int lastXPValue = 0;
    private bool hasGainedXPOnce = false;

    private void Start()
    {
        UpdateUI(PlayerProgression.Instance.CurrentXP, PlayerProgression.Instance.XPToNextLevel);
        UpdateLevel(PlayerProgression.Instance.CurrentLevel);

        PlayerProgression.Instance.OnXPChanged += UpdateUI;
        PlayerProgression.Instance.OnLevelChanged += UpdateLevel;

        lastXPValue = PlayerProgression.Instance.CurrentXP;

        if (xpGainText != null)
        {
            xpGainText.alpha = 0f;
        }
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
            xpBarText.text = $"{currentXP}/{requiredXP}";
        }

        int gainedXP = currentXP - lastXPValue;
        if (gainedXP > 0)
        {
            if (hasGainedXPOnce)
            {
                ShowXPGainText($"+{gainedXP} XP");
            }
            else
            {
                hasGainedXPOnce = true;
            }
        }


        lastXPValue = currentXP;
    }

    private void UpdateLevel(int newLevel)
    {
        if (levelText != null)
        {
            levelText.text = $"{newLevel}";
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
            canvasGroup.DOFade(1f, fadeDuration)
                .SetUpdate(true); // runs even during pause
        }
        else
        {
            canvasGroup.DOFade(0f, fadeDuration)
                .SetUpdate(true) // runs even during pause
                .OnComplete(() => gameObject.SetActive(false));
        }
    }


    private void ShowXPGainText(string text)
    {
        if (xpGainText == null || xpGainRect == null || xpGainStartReference == null) return;

        xpGainText.text = text;
        xpGainText.alpha = 0f;
        xpGainText.gameObject.SetActive(true);

        // Reset position to prefab-defined reference object
        xpGainRect.anchoredPosition = xpGainStartReference.anchoredPosition;

        xpGainText.DOKill();
        xpGainRect.DOKill();

        Sequence seq = DOTween.Sequence();
        seq.Append(xpGainText.DOFade(1f, 0.3f))
           .Join(xpGainRect.DOAnchorPosX(xpGainStartReference.anchoredPosition.x + gainTextOffset * 0.5f, 0.3f).SetEase(Ease.OutQuad))
           .AppendInterval(gainTextDuration - 0.6f)
           .Append(xpGainText.DOFade(0f, 0.3f))
           .Join(xpGainRect.DOAnchorPosX(xpGainStartReference.anchoredPosition.x + gainTextOffset, 0.3f).SetEase(Ease.InQuad));
    }

    private void OnDestroy()
    {
        if (PlayerProgression.Instance == null) return;

        PlayerProgression.Instance.OnXPChanged -= UpdateUI;
        PlayerProgression.Instance.OnLevelChanged -= UpdateLevel;
    }
}
