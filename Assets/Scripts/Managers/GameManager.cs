using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("References (UI, Prefabs)")]
    [SerializeField] private GameView gameView;
    [SerializeField] private GameObject piniataPrefab;
    [SerializeField] private Transform piniataSpawnPoint;
    [SerializeField] private Transform piniataDestroyPoint;

    [Header("Piniata Variants (Sprite + Hit Particle)")]
    [SerializeField] private PiniataVariant[] piniataVariants;

    [Header("Spawn Intervals (Seconds)")]
    [SerializeField] private float spawnIntervalMin = 1f;
    [SerializeField] private float spawnIntervalMax = 2f;

    [Header("Gravity Settings")]
    [SerializeField] private float globalGravityScale = 0.5f;
    [SerializeField] private float speedIncreaseAmount = 0.5f;
    private float nextSpeedIncreaseTime = 30f;

    [Header("Piñata Smash Particle")]
    [SerializeField] private ParticleSystem piniataSmashParticle;

    [Header("Audio & Screen Shake")]
    [SerializeField] private AudioManager audioManager; // MUST reference the new AudioManager
    [SerializeField] private ScreenShake screenShake;
    [SerializeField] private float bombShakeIntensity = 0.4f;
    [SerializeField] private float bombShakeDuration = 0.5f;
    [SerializeField] private float criticalShakeIntensity = 0.3f;
    [SerializeField] private float criticalShakeDuration = 0.4f;

    private List<PiniataController> activePiniatas = new List<PiniataController>();
    private GameModel gameModel;
    private int destroyedPiniataCount;

    // Bomb / Critical logic
    private int piniatasOpenedSinceLastBomb;
    private const int minPiniatasBeforeBombChance = 3;
    private const float BombGrantChance = 0.2f;

    private int piniatasOpenedSinceLastCritical;
    private const int MinPiniatasBeforeCriticalChance = 5;
    private const float CriticalGrantChance = 0.3f;

    public event Action<int> OnScoreUpdated;
    public event Action<int> OnBombCountUpdated;
    public event Action<int> OnCriticalCountUpdated;

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
        gameView.UpdatePiniatasDestroyed(destroyedPiniataCount);

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
            Debug.LogWarning("Spawn/Destroy points not assigned!");
            return;
        }

        float randomX = UnityEngine.Random.Range(-2f, 2f);
        Vector3 spawnPos = new Vector3(
            piniataSpawnPoint.position.x + randomX,
            piniataSpawnPoint.position.y,
            0f
        );

        GameObject piObj = Instantiate(piniataPrefab, spawnPos, Quaternion.identity);

        var sr = piObj.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.flipX = (UnityEngine.Random.value > 0.5f);
        }

        if (piniataVariants != null && piniataVariants.Length > 0)
        {
            int idx = UnityEngine.Random.Range(0, piniataVariants.Length);
            var chosen = piniataVariants[idx];
            if (sr && chosen.piniataSprite)
            {
                sr.sprite = chosen.piniataSprite;
            }
            var ctrlVar = piObj.GetComponent<PiniataController>();
            if (ctrlVar && chosen.hitParticle)
            {
                ctrlVar.hitParticlePrefab = chosen.hitParticle;
            }
        }

        int required = UnityEngine.Random.Range(1, 31);
        var controller = piObj.GetComponent<PiniataController>();
        if (controller != null)
        {
            controller.Initialize(this, required, piniataDestroyPoint);

            var rb = piObj.GetComponent<Rigidbody2D>();
            if (rb)
            {
                rb.bodyType = RigidbodyType2D.Dynamic;
                rb.gravityScale = globalGravityScale;
            }

            activePiniatas.Add(controller);
        }
        else
        {
            Debug.LogError("No PiniataController on prefab!");
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
            // 1) Random piñata click SFX
            audioManager?.PlayPiniataClickSound();

            bool isOpened = ctrl.HandleClick();
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
        gameModel.Score += ctrl.RequiredClicks;
        OnScoreUpdated?.Invoke(gameModel.Score);
        gameView.UpdateScore(gameModel.Score);

        destroyedPiniataCount++;
        gameView.UpdatePiniatasDestroyed(destroyedPiniataCount);

        if (destroyedPiniataCount % 15 == 0)
        {
            gameModel.Timer += 30f;
            ShowNotification("+30s bonus time!", 2f);
        }

        piniatasOpenedSinceLastBomb++;
        piniatasOpenedSinceLastCritical++;

        TryGrantBomb();
        TryGrantCritical();

        RemovePiniata(ctrl, true);
    }

    public void RemovePiniata(PiniataController ctrl, bool destroyedByUser)
    {
        if (activePiniatas.Contains(ctrl))
        {
            activePiniatas.Remove(ctrl);
        }
        if (destroyedByUser)
        {
            // Smash particle
            if (piniataSmashParticle != null)
            {
                Instantiate(piniataSmashParticle, ctrl.transform.position, Quaternion.identity);
            }
            // If exactly one piñata is destroyed => we can play smash sound
            // But if multiple are destroyed, we handle it after the loop
            // So we won't do it here for mass-destructions
        }
        Destroy(ctrl.gameObject);
    }

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

        // 1) Bomb SFX
        audioManager?.PlayBombSound();
        // 2) Screen Shake
        screenShake?.Shake(bombShakeIntensity, bombShakeDuration);

        gameModel.BombCount--;
        OnBombCountUpdated?.Invoke(gameModel.BombCount);

        // If multiple piñatas => spawn smash for each
        bool anyDestroyed = (activePiniatas.Count > 0);

        foreach (var ctrl in activePiniatas)
        {
            if (piniataSmashParticle != null)
            {
                Instantiate(piniataSmashParticle, ctrl.transform.position, Quaternion.identity);
            }
            gameModel.Score += ctrl.RequiredClicks;
            Destroy(ctrl.gameObject);
        }
        activePiniatas.Clear();

        // If we actually destroyed piñatas => single smash sound
        if (anyDestroyed)
        {
            audioManager?.PlayPiniataSmashSound();
        }

        OnScoreUpdated?.Invoke(gameModel.Score);
        gameView.UpdateScore(gameModel.Score);
    }

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

        // 1) Critical SFX
        audioManager?.PlayCriticalSound();
        // 2) Screen Shake
        screenShake?.Shake(criticalShakeIntensity, criticalShakeDuration);

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

        bool destroyedMultiple = (toRemove.Count > 0);

        foreach (var c in toRemove)
        {
            activePiniatas.Remove(c);
            Destroy(c.gameObject);
        }

        // If multiple piñatas destroyed => single smash sound
        if (destroyedMultiple)
        {
            audioManager?.PlayPiniataSmashSound();
        }

        gameView.UpdateScore(gameModel.Score);
        OnScoreUpdated?.Invoke(gameModel.Score);
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
                // Increase gravity => faster falling
                globalGravityScale += speedIncreaseAmount;
                nextSpeedIncreaseTime += 30f;

                ShowNotification($"Piniatas are now falling faster! (gravity={globalGravityScale})", 2f);

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

    public void ShowNotification(string message, float duration)
    {
        gameView.ShowNotificationsWithTimer(duration, message);
    }
}
