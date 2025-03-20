using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PiniataController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI clicksText;
    
    private GameManager gameManager;
    private RectTransform rt;
    private RectTransform destroyPos; 
    private float fallSpeed;

    public int RequiredClicks { get; set; }
    public int CurrentClicks { get; private set; } = 0;

    public void Initialize(GameManager manager, int required, float speed, RectTransform destroyBoundary)
    {
        gameManager = manager;
        RequiredClicks = required;
        fallSpeed = speed;
        destroyPos = destroyBoundary;

        rt = GetComponent<RectTransform>();
        UpdateClicksText();
    }

    private void Update()
    {
        if (rt == null) return;

        Vector2 pos = rt.anchoredPosition;
        pos.y -= fallSpeed * Time.deltaTime;
        rt.anchoredPosition = pos;

        if (destroyPos != null)
        {
            if (pos.y < destroyPos.anchoredPosition.y)
            {
                // Piñata was never opened
                gameManager.RemovePiñata(this, false);
            }
        }
        else
        {
            // Fallback: if y < -1000
            if (pos.y < -1000f)
            {
                gameManager.RemovePiñata(this, false);
            }
        }
    }

    public bool HandleClick(float maxClickInterval = 2f)
    {
        CurrentClicks++;
        UpdateClicksText();
        return (CurrentClicks >= RequiredClicks);
    }

    public void UpdateClicksText()
    {
        if (clicksText != null)
        {
            int remain = RequiredClicks - CurrentClicks;
            if (remain < 0) remain = 0;
            clicksText.text = remain.ToString();
        }
    }
}