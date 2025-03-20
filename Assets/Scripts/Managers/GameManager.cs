using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private GameView gameView;
    [SerializeField] private GameObject piniataPrefab;
    [SerializeField] private Transform piniataSpawnPoint;
    [SerializeField] private Transform piniataDestroyPoint;

    [Header("Variants: matching Piniata sprites + hit-particle systems")]
    [SerializeField] private PiniataVariant[] piniataVariants;

    [Header("Piñata Rain Settings")]
    [Tooltip("Time range between each spawn (seconds)")]
    [SerializeField] private float spawnIntervalMin = 1f;
    [SerializeField] private float spawnIntervalMax = 2f;

    [Tooltip("Initial gravity scale for piñatas (so you can tweak slow vs. fast)")]
    [SerializeField] private float globalGravityScale = 0.5f;

    [Tooltip("Each 30s we add this to gravityScale => faster falling")]
    [SerializeField] private float speedIncreaseAmount = 0.5f;

    private float nextSpeedIncreaseTime = 30f;

    [Header("Piñata Smash Particle")]
    [Tooltip("Particle system to spawn whenever a piñata is fully destroyed by user (click or bomb).")]
    [SerializeField] private ParticleSystem piniataSmashParticle;

    private List<PiniataController> activePiniatas = new List<PiniataController>();
    private GameModel gameModel;

    private int destroyedPiniataCount;

    #region Bomb/Critical logic

    private int piniatasOpenedSinceLastBomb;
    private const int minPiniatasBeforeBombChance = 3;
    private const float BombGrantChance = 0.2f;

    private int piniatasOpenedSinceLastCritical;
    private const int MinPiniatasBeforeCriticalChance = 5;
    private const float CriticalGrantChance = 0.3f;

    #endregion

    #region Events
    public event Action<int> OnScoreUpdated;
    public event Action<int> OnBombCountUpdated;
    public event Action<int> OnCriticalCountUpdated;
    #endregion

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        gameModel = new GameModel { Timer = 120f };

        gameView.Init(this);
        gameView.UpdateScore(gameModel.Score);
        gameView.UpdateTimer(gameModel.Timer);
        gameView.UpdateBombUI(gameModel.BombCount);
        gameView.UpdateCriticalUI(gameModel.CriticalCount);
        gameView.UpdatePiniatasDestroyed(destroyedPiniataCount); // 0 at start

        StartCoroutine(SpawnPiniatasRoutine());
    }

    private void Update()
    {
        UpdateTimer(Time.deltaTime);
    }

    private IEnumerator SpawnPiniatasRoutine()
    {
        while (gameModel.Timer > 0)
        {
            float waitTime = UnityEngine.Random.Range(spawnIntervalMin, spawnIntervalMax);
            yield return new WaitForSeconds(waitTime);

            SpawnFallingPiniata();
        }
    }

    private void SpawnFallingPiniata()
    {
        if (!piniataSpawnPoint || !piniataDestroyPoint)
        {
            Debug.LogWarning("Spawn or Destroy point not assigned!");
            return;
        }

        float randomX = UnityEngine.Random.Range(-2f, 2f);
        Vector3 spawnPos = new Vector3(
            piniataSpawnPoint.position.x + randomX,
            piniataSpawnPoint.position.y,
            0f
        );

        GameObject piObj = Instantiate(piniataPrefab, spawnPos, Quaternion.identity);

        // Random flip X
        var sr = piObj.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.flipX = (UnityEngine.Random.value > 0.5f);
        }

        // If we have variant arrays => random color + particle
        if (piniataVariants != null && piniataVariants.Length > 0)
        {
            int idx = UnityEngine.Random.Range(0, piniataVariants.Length);
            var chosen = piniataVariants[idx];
            if (sr != null && chosen.piniataSprite)
            {
                sr.sprite = chosen.piniataSprite;
            }
            var ctrlVar = piObj.GetComponent<PiniataController>();
            if (ctrlVar != null && chosen.hitParticle)
            {
                ctrlVar.hitParticlePrefab = chosen.hitParticle;
            }
        }

        // Piñata requires random clicks
        int required = UnityEngine.Random.Range(1, 31);

        var controller = piObj.GetComponent<PiniataController>();
        if (controller != null)
        {
            controller.Initialize(this, required, piniataDestroyPoint);

            // Switch to dynamic so it falls with gravity
            var rb = piObj.GetComponent<Rigidbody2D>();
            if (rb)
            {
                rb.bodyType = RigidbodyType2D.Dynamic;
                rb.gravityScale = globalGravityScale; // set initial fall speed
            }

            activePiniatas.Add(controller);
        }
        else
        {
            Debug.LogError("No PiniataController found on the prefab!");
        }
    }

    public void OnPiniataClicked(PiniataController ctrl)
    {
        int actualClickIncrement = 1;
        if (gameModel.NextCriticalValue > 0)
        {
            actualClickIncrement += gameModel.NextCriticalValue;
            gameModel.NextCriticalValue = 0;
            ShowNotification($"Critical hit added {actualClickIncrement - 1} extra clicks!", 2f);
        }

        for (int i = 0; i < actualClickIncrement; i++)
        {
            bool isOpened = ctrl.HandleClick();
            // Spawn hit effect + bounce
            ctrl.SpawnHitParticle();
            ctrl.BouncePiniata();

            if (isOpened)
            {
                PiniataFullyOpened(ctrl);
                break;
            }
        }
    }

    private void PiniataFullyOpened(PiniataController ctrl)
    {
        // Score
        gameModel.Score += ctrl.RequiredClicks;
        OnScoreUpdated?.Invoke(gameModel.Score);
        gameView.UpdateScore(gameModel.Score);

        // Count destroyed
        destroyedPiniataCount++;
        gameView.UpdatePiniatasDestroyed(destroyedPiniataCount);

        // 30s each 15 destroyed
        if (destroyedPiniataCount % 15 == 0)
        {
            gameModel.Timer += 30f;
            ShowNotification("+30s bonus time!", 2f);
        }

        piniatasOpenedSinceLastBomb++;
        piniatasOpenedSinceLastCritical++;

        TryGrantBomb();
        TryGrantCritical();

        // We remove the piñata and pass "true" => destroyed by user => show smash
        RemovePiniata(ctrl, true);
    }

    public void RemovePiniata(PiniataController ctrl, bool addScore)
    {
        if (activePiniatas.Contains(ctrl))
        {
            activePiniatas.Remove(ctrl);
        }
        if (addScore)
        {
            // user destroyed => spawn smash particle at that piñata's position
            if (piniataSmashParticle != null)
            {
                Instantiate(piniataSmashParticle, ctrl.transform.position, Quaternion.identity);
            }

            // optional leftover score
            gameModel.Score += ctrl.RequiredClicks;
            OnScoreUpdated?.Invoke(gameModel.Score);
            gameView.UpdateScore(gameModel.Score);
        }
        Destroy(ctrl.gameObject);
    }

    #region Bomb Logic

    private void TryGrantBomb()
    {
        if (piniatasOpenedSinceLastBomb >= minPiniatasBeforeBombChance)
        {
            float roll = UnityEngine.Random.value;
            if (roll <= BombGrantChance)
            {
                gameModel.BombCount++;
                OnBombCountUpdated?.Invoke(gameModel.BombCount);
                ShowNotification("You've won a BOMB!", 2f);
                piniatasOpenedSinceLastBomb = 0;
            }
        }
    }

    public void OnBombButtonClicked()
    {
        if (gameModel.BombCount <= 0) return;

        gameModel.BombCount--;
        OnBombCountUpdated?.Invoke(gameModel.BombCount);

        // For each piñata => user destroyed => spawn smash particle
        foreach (var ctrl in activePiniatas)
        {
            // spawn smash
            if (piniataSmashParticle != null)
            {
                Instantiate(piniataSmashParticle, ctrl.transform.position, Quaternion.identity);
            }

            gameModel.Score += ctrl.RequiredClicks;
            Destroy(ctrl.gameObject);
        }
        activePiniatas.Clear();

        OnScoreUpdated?.Invoke(gameModel.Score);
        gameView.UpdateScore(gameModel.Score);
    }

    #endregion

    #region Critical Logic

    private void TryGrantCritical()
    {
        if (piniatasOpenedSinceLastCritical >= MinPiniatasBeforeCriticalChance)
        {
            float roll = UnityEngine.Random.value;
            if (roll <= CriticalGrantChance)
            {
                gameModel.CriticalCount++;
                OnCriticalCountUpdated?.Invoke(gameModel.CriticalCount);
                ShowNotification("You've won a CRITICAL HIT!", 2f);
                piniatasOpenedSinceLastCritical = 0;
            }
        }
    }

    public void OnCriticalButtonClicked()
    {
        if (gameModel.CriticalCount <= 0) return;

        gameModel.CriticalCount--;
        OnCriticalCountUpdated?.Invoke(gameModel.CriticalCount);

        int randomX = UnityEngine.Random.Range(1, 6);
        ShowNotification($"All Piniatas reduced by {randomX} clicks!", 2.5f);

        List<PiniataController> toRemove = new List<PiniataController>();
        foreach (var ctrl in activePiniatas)
        {
            ctrl.RequiredClicks -= randomX;
            if (ctrl.RequiredClicks <= ctrl.CurrentClicks)
            {
                // spawn smash
                if (piniataSmashParticle != null)
                {
                    Instantiate(piniataSmashParticle, ctrl.transform.position, Quaternion.identity);
                }

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

    #endregion

    #region Speed Increase / Timer

    private float nextSpeedCheck;

    private void UpdateTimer(float deltaTime)
    {
        if (gameModel.Timer > 0)
        {
            gameModel.Timer -= deltaTime;
            gameView.UpdateTimer(gameModel.Timer);

            float elapsed = 120f - gameModel.Timer;
            if (elapsed >= nextSpeedIncreaseTime)
            {
                // Increase gravity => faster falling
                globalGravityScale += speedIncreaseAmount;
                nextSpeedIncreaseTime += 30f;

                ShowNotification($"Piniatas are now falling faster! (gravity={globalGravityScale})", 2f);

                // Update existing piñatas
                foreach (var ctrl in activePiniatas)
                {
                    var rb = ctrl.GetComponent<Rigidbody2D>();
                    if (rb)
                    {
                        rb.gravityScale = globalGravityScale;
                    }
                }
            }
        }
        if (gameModel.Timer <= 0)
        {
            gameModel.Timer = 0;
            gameView.UpdateTimer(gameModel.Timer);
        }
    }

    #endregion

    // Removed all cooldown references

    public void ShowNotification(string message, float duration)
    {
        gameView.ShowNotificationsWithTimer(duration, message);
    }
}
