using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private GameView gameView;
    [SerializeField] private GameObject piniataPrefab;
    [SerializeField] private Transform canvasTransform;
    [SerializeField] private RectTransform piniataDestroyPos;

    [Header("Piñata Rain Settings")]
    [SerializeField] private float spawnIntervalMin = 1f;
    [SerializeField] private float spawnIntervalMax = 3f;

    // Increase this so Piñatas actually move visually faster.
    [SerializeField] private float baseFallSpeed = 300f;

    // Each 30s, we increase speed by this amount.
    [SerializeField] private float speedIncrease = 100f;

    private float nextSpeedIncreaseTime = 30f;
    private float speedMultiplier = 0f;

    // Track all Piñatas on screen
    private List<PiniataController> activePiniatas = new List<PiniataController>();

    // Main model
    private GameModel gameModel;

    #region Bomb/Critical random logic

    private int piñatasOpenedSinceLastBomb = 0;
    private const int minPiniatasBeforeBombChance = 3;
    private const float BombGrantChance = 0.2f;

    private int piñatasOpenedSinceLastCritical = 0;
    private const int MinPiniatasBeforeCriticalChance = 5;
    private const float CriticalGrantChance = 0.3f;

    #endregion

    private int destroyedPiñataCount = 0;

    #region Events

    public event Action<int> OnScoreUpdated;
    public event Action<bool, float> OnCooldownTriggered;
    public event Action<int> OnBombCountUpdated;
    public event Action<int> OnCriticalCountUpdated;

    #endregion

    private bool isCooldownActive = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // Start with 120s
        gameModel = new GameModel { Timer = 120f };

        // Init UI
        gameView.Init(this);
        gameView.UpdateScore(gameModel.Score);
        gameView.UpdateTimer(gameModel.Timer);
        gameView.UpdateBombUI(gameModel.BombCount);
        gameView.UpdateCriticalUI(gameModel.CriticalCount);

        // Start spawning Piñatas
        StartCoroutine(SpawnPiñatasRoutine());
    }

    private void Update()
    {
        UpdateTimer(Time.deltaTime);
    }

    private IEnumerator SpawnPiñatasRoutine()
    {
        while (gameModel.Timer > 0)
        {
            float waitTime = UnityEngine.Random.Range(spawnIntervalMin, spawnIntervalMax);
            yield return new WaitForSeconds(waitTime);

            SpawnFallingPiñata();
        }
    }

    private void SpawnFallingPiñata()
    {
        // We spawn them well above top. e.g., y=1200 for a 1280-high reference.
        // If your screen is bigger, the Canvas Scaler will scale accordingly.
        float randomX = UnityEngine.Random.Range(-300f, 300f);
        Vector2 spawnPos = new Vector2(randomX, 1200f);

        GameObject piñataObj = Instantiate(piniataPrefab, canvasTransform);
        RectTransform rt = piñataObj.GetComponent<RectTransform>();
        rt.anchoredPosition = spawnPos;

        int required = UnityEngine.Random.Range(1, 31);
        float piñataSpeed = baseFallSpeed + speedMultiplier * speedIncrease;

        PiniataController ctrl = piñataObj.GetComponent<PiniataController>();
        // Pass destroyPos so it knows where to remove itself
        ctrl.Initialize(this, required, piñataSpeed, piniataDestroyPos);

        Button piñataButton = piñataObj.GetComponent<Button>();
        if (piñataButton != null)
        {
            piñataButton.onClick.AddListener(() => OnPiniataClicked(ctrl));
            piñataButton.AddSquishEffect(0.8f, 0.1f);
        }

        activePiniatas.Add(ctrl);
    }

    public void OnPiniataClicked(PiniataController ctrl)
    {
        if (isCooldownActive) return;

        int actualClickIncrement = 1;
        if (gameModel.NextCriticalValue > 0)
        {
            actualClickIncrement += gameModel.NextCriticalValue;
            gameModel.NextCriticalValue = 0;
            ShowNotification($"Critical hit added {actualClickIncrement - 1} extra clicks!", 2f);
        }

        for (int i = 0; i < actualClickIncrement; i++)
        {
            bool isOpened = ctrl.HandleClick(2f);
            if (isOpened)
            {
                PiñataFullyOpened(ctrl);
                break;
            }
        }
    }

    private void PiñataFullyOpened(PiniataController ctrl)
    {
        gameModel.Score += ctrl.RequiredClicks;
        OnScoreUpdated?.Invoke(gameModel.Score);
        gameView.UpdateScore(gameModel.Score);

        destroyedPiñataCount++;
        if (destroyedPiñataCount % 10 == 0)
        {
            gameModel.Timer += 30f;
            ShowNotification("+30s bonus time!", 2f);
        }

        piñatasOpenedSinceLastBomb++;
        piñatasOpenedSinceLastCritical++;
        TryGrantBomb();
        TryGrantCritical();

        RemovePiñata(ctrl, false);
    }

    public void RemovePiñata(PiniataController ctrl, bool addScore)
    {
        if (activePiniatas.Contains(ctrl))
            activePiniatas.Remove(ctrl);

        if (addScore)
        {
            gameModel.Score += ctrl.RequiredClicks;
            OnScoreUpdated?.Invoke(gameModel.Score);
            gameView.UpdateScore(gameModel.Score);
        }
        Destroy(ctrl.gameObject);
    }

    private void TryGrantBomb()
    {
        if (piñatasOpenedSinceLastBomb >= minPiniatasBeforeBombChance)
        {
            float roll = UnityEngine.Random.value;
            if (roll <= BombGrantChance)
            {
                gameModel.BombCount++;
                OnBombCountUpdated?.Invoke(gameModel.BombCount);
                ShowNotification("You've won a BOMB!", 2f);
                piñatasOpenedSinceLastBomb = 0;
            }
        }
    }

    public void OnBombButtonClicked()
    {
        if (gameModel.BombCount <= 0) return;

        gameModel.BombCount--;
        OnBombCountUpdated?.Invoke(gameModel.BombCount);

        foreach (var ctrl in activePiniatas)
        {
            gameModel.Score += ctrl.RequiredClicks;
            Destroy(ctrl.gameObject);
        }
        activePiniatas.Clear();

        OnScoreUpdated?.Invoke(gameModel.Score);
        gameView.UpdateScore(gameModel.Score);
    }

    private void TryGrantCritical()
    {
        if (piñatasOpenedSinceLastCritical >= MinPiniatasBeforeCriticalChance)
        {
            float roll = UnityEngine.Random.value;
            if (roll <= CriticalGrantChance)
            {
                gameModel.CriticalCount++;
                OnCriticalCountUpdated?.Invoke(gameModel.CriticalCount);
                ShowNotification("You've won a CRITICAL HIT!", 2f);
                piñatasOpenedSinceLastCritical = 0;
            }
        }
    }

    public void OnCriticalButtonClicked()
    {
        if (gameModel.CriticalCount <= 0) return;

        gameModel.CriticalCount--;
        OnCriticalCountUpdated?.Invoke(gameModel.CriticalCount);

        int randomX = UnityEngine.Random.Range(1, 6);
        ShowNotification($"All Piñatas reduced by {randomX} clicks!", 2.5f);

        List<PiniataController> toRemove = new List<PiniataController>();

        foreach (var ctrl in activePiniatas)
        {
            ctrl.RequiredClicks -= randomX;
            if (ctrl.RequiredClicks <= ctrl.CurrentClicks)
            {
                gameModel.Score += ctrl.RequiredClicks + randomX;
                toRemove.Add(ctrl);
            }
            else
            {
                ctrl.UpdateClicksText();
            }
        }

        foreach (var c in toRemove)
        {
            activePiniatas.Remove(c);
            Destroy(c.gameObject);
        }

        gameView.UpdateScore(gameModel.Score);
        OnScoreUpdated?.Invoke(gameModel.Score);
    }

    private IEnumerator CoHandleCooldown(float duration)
    {
        isCooldownActive = true;
        OnCooldownTriggered?.Invoke(true, duration);

        yield return new WaitForSeconds(duration);

        isCooldownActive = false;
        OnCooldownTriggered?.Invoke(false, 0f);
    }

    private void UpdateTimer(float deltaTime)
    {
        if (gameModel.Timer > 0)
        {
            gameModel.Timer -= deltaTime;
            gameView.UpdateTimer(gameModel.Timer);

            float elapsed = 120f - gameModel.Timer;
            if (elapsed >= nextSpeedIncreaseTime)
            {
                speedMultiplier += 1f;
                nextSpeedIncreaseTime += 30f;
                ShowNotification($"Piñatas are now falling faster! (x{(int) speedMultiplier})", 2f);
            }
        }
        if (gameModel.Timer <= 0)
        {
            gameModel.Timer = 0;
            gameView.UpdateTimer(gameModel.Timer);
        }
    }

    public void ShowNotification(string message, float duration)
    {
        gameView.ShowNotificationsWithTimer(duration, message);
    }
}
