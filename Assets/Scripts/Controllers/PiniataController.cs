using UnityEngine;
using TMPro;
using DG.Tweening;

[RequireComponent(typeof(Rigidbody2D))]
public class PiniataController : MonoBehaviour
{
    [Header("3D Text to display remaining clicks")]
    [SerializeField] private TextMeshPro clicksText;

    public ParticleSystem hitParticlePrefab;

    private PiniataModel model;
    private GameManager gameManager;
    private Transform destroyPoint;
    private Rigidbody2D rb2d;
    private Tween activeSquishTween;

    public int RequiredClicks
    {
        get => model.RequiredClicks;
        set => model.RequiredClicks = value;
    }
    public int CurrentClicks => model.CurrentClicks;

    [Header("Bounce Settings")]
    [Tooltip("How strong the upward force is when user clicks")]
    [SerializeField] private float bounceForce = 3f;

    public void Initialize(GameManager manager, int required, Transform _destroyPoint)
    {
        gameManager = manager;
        destroyPoint = _destroyPoint;
        model = new PiniataModel(required);

        rb2d = GetComponent<Rigidbody2D>();
        UpdateClicksText();
    }

    private void Update()
    {
        if (destroyPoint && transform.position.y < destroyPoint.position.y)
        {
            gameManager.RemovePiniata(this, false);
        }
    }

    private void OnMouseDown()
    {
        if (gameManager != null)
        {
            gameManager.OnPiniataClicked(this);
        }
    }

    public bool HandleClick()
    {
        model.CurrentClicks++;
        UpdateClicksText();
        return (model.CurrentClicks >= model.RequiredClicks);
    }

    public void UpdateClicksText()
    {
        if (clicksText != null)
        {
            int remain = model.RequiredClicks - model.CurrentClicks;
            if (remain < 0) remain = 0;
            clicksText.text = remain.ToString();
        }
    }

    public void SpawnHitParticle()
    {
        if (hitParticlePrefab)
        {
            Instantiate(hitParticlePrefab, transform.position, Quaternion.identity);
        }
    }

    public void BouncePiniata()
    {
        if (activeSquishTween != null && activeSquishTween.IsActive())
        {
            activeSquishTween.Kill();
        }

        Vector3 originalScale = transform.localScale;
        activeSquishTween = transform.DOScale(originalScale * 0.8f, 0.1f)
            .OnComplete(() =>
            {
                transform.DOScale(originalScale, 0.1f).SetEase(Ease.OutBack);
            });

        if (rb2d)
        {
            rb2d.velocity = new Vector2(rb2d.velocity.x, 0f);
            rb2d.AddForce(new Vector2(0, bounceForce), ForceMode2D.Impulse);
        }
    }

    private void OnDestroy()
    {
        if (activeSquishTween != null && activeSquishTween.IsActive())
        {
            activeSquishTween.Kill();
        }

        DOTween.Kill(transform);
    }
}
