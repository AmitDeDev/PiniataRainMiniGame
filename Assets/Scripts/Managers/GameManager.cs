using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("General References")]
    [SerializeField] private GameView gameView;
    [SerializeField] private ProgressionView progressionView;
    [SerializeField] private GameObject piniataPrefab;
    [SerializeField] private Transform piniataSpawnPoint;
    [SerializeField] private Transform piniataDestroyPoint;
    [SerializeField] private ScreenShake cameraShake;

    [Header("Piniata Variants (Sprite + Hit Particle)")]
    [SerializeField] private PiniataVariant[] piniataVariants;

    [Header("Special Piniatas")]
    [Tooltip("Golden Piniata Prefab")]
    [SerializeField] private GameObject goldenPiniataPrefab;
    [Tooltip("Black Piniata Prefab")]
    [SerializeField] private GameObject blackPiniataPrefab;

    [Header("Spawn Intervals")]
    [SerializeField] private float spawnIntervalMin = 1f;
    [SerializeField] private float spawnIntervalMax = 2f;

    [Header("Golden / Black Timers")]
    [Tooltip("Spawn a golden piniata once a minute (or random in a 60s window).")]
    [SerializeField] private float goldenSpawnDelay = 60f;
    [Tooltip("Spawn black piniata once every  seconds.")]
    [SerializeField] private float blackSpawnDelay = 45f;

    [Header("Gravity Settings")]
    [SerializeField] private float globalGravityScale = 0.5f;
    [SerializeField] private float speedIncreaseAmount = 0.5f;
    private float nextSpeedIncreaseTime = 30f;

    [Header("Piniata Smash Particle")]
    [SerializeField] private ParticleSystem piniataSmashParticle;
    
    [Header("Popups / Flow")]
    [SerializeField] private float GameOverPopupDelay = 1f;
    [SerializeField] private float multiplierDuration = 15f;

    [Header("Audio")] 
    [SerializeField] private AudioManager audioManager;

    [Header("Scene Manager")] 
    [SerializeField] private SceneLoader sceneLoader;
    
    #region Score multiplier logic
    private bool goldenMultiplierActive;
    private float goldenTimerLeft;
    private float goldenMultiplierValue = 1f;
    #endregion

    private List<PiniataController> activePiniatas = new List<PiniataController>();
    private GameModel gameModel;
    private Vibartions vibrate;
    private int destroyedPiniataCount;

    private bool isGameOver;

    #region Bomb / Critical thresholds
    private int piniatasOpenedSinceLastBomb;
    private int piniatasOpenedSinceLastCritical;
    
    private int bombsUsedInSession;
    private int criticalUsedInSession;
    #endregion

    #region Events
    public event Action<int> OnScoreUpdated;
    public event Action<int> OnBombCountUpdated;
    public event Action<int> OnCriticalCountUpdated;
    #endregion

    private void Awake()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
        
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        vibrate = new Vibartions();
        gameModel = new GameModel { Timer = 120f };
        gameView.Init(this);

        gameView.UpdateScore(gameModel.Score);
        gameView.UpdateTimer(gameModel.Timer);
        gameView.UpdateBombUI(gameModel.BombCount);
        gameView.UpdateCriticalUI(gameModel.CriticalCount);
        gameView.UpdatePiniatasDestroyed(destroyedPiniataCount);

        goldenMultiplierValue = 1f;
        goldenTimerLeft = 0f;

        StartCoroutine(SpawnPiniatasRoutine());
        StartCoroutine(SpawnGoldenPiniataRoutine());
        StartCoroutine(SpawnBlackPiniataRoutine());
    }

    private void Update()
    {
        if (isGameOver) return;
        UpdateTimer(Time.deltaTime);
        UpdateGoldenMultiplier(Time.deltaTime);
    }

    //====================
    //  SPAWN ROUTINES
    //====================

    private IEnumerator SpawnPiniatasRoutine()
    {
        while (gameModel.Timer > 0 && !isGameOver)
        {
            float waitTime = UnityEngine.Random.Range(spawnIntervalMin, spawnIntervalMax);
            yield return new WaitForSeconds(waitTime);

            if (isGameOver) yield break;
            SpawnFallingPiniata();
        }
    }

    private IEnumerator SpawnGoldenPiniataRoutine()
    {
        yield return new WaitForSeconds(UnityEngine.Random.Range(30f, goldenSpawnDelay));
        if (!isGameOver && gameModel.Timer > 0)
        {
            SpawnSpecialPiniata(goldenPiniataPrefab);
        }

        yield return new WaitForSeconds(goldenSpawnDelay);
        if (!isGameOver && gameModel.Timer > 0)
        {
            SpawnSpecialPiniata(goldenPiniataPrefab);
        }
    }

    private IEnumerator SpawnBlackPiniataRoutine()
    {
        while (!isGameOver && gameModel.Timer > 0)
        {
            yield return new WaitForSeconds(blackSpawnDelay);
            if (isGameOver || gameModel.Timer <= 0) yield break;
            SpawnSpecialPiniata(blackPiniataPrefab);
        }
    }

    private void SpawnFallingPiniata()
    {
        if (!piniataSpawnPoint || !piniataDestroyPoint) return;

        float randomX = UnityEngine.Random.Range(-2f, 2f);
        Vector3 spawnPos = new Vector3(
            piniataSpawnPoint.position.x + randomX,
            piniataSpawnPoint.position.y,
            0f
        );
        GameObject piObj = Instantiate(piniataPrefab, spawnPos, Quaternion.identity);

        var sr = piObj.GetComponent<SpriteRenderer>();
        if (sr) sr.flipX = (UnityEngine.Random.value > 0.5f);

        if (piniataVariants != null && piniataVariants.Length > 0)
        {
            int idx = UnityEngine.Random.Range(0, piniataVariants.Length);
            var chosen = piniataVariants[idx];
            if (chosen.piniataSprite && sr)
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
    }

    private void SpawnSpecialPiniata(GameObject specialPrefab)
    {
        if (!piniataSpawnPoint || !piniataDestroyPoint || !specialPrefab) return;

        float randomX = UnityEngine.Random.Range(-2f, 2f);
        Vector3 spawnPos = new Vector3(
            piniataSpawnPoint.position.x + randomX,
            piniataSpawnPoint.position.y,
            0f
        );
        GameObject piObj = Instantiate(specialPrefab, spawnPos, Quaternion.identity);

        int required = 0;
        string lowerName = piObj.name.ToLower();
        if (lowerName.Contains("gold"))
        {
            required = UnityEngine.Random.Range(5, 21);
        }
        else if (lowerName.Contains("black"))
        {
            required = UnityEngine.Random.Range(1, 3);
        }

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
    }

    //====================
    //  PIÑATA CLICK
    //====================

    public void OnPiniataClicked(PiniataController ctrl)
    {
        if (isGameOver) return;
        
        vibrate.MediumVibration();
        
        int actualClickIncrement = 1;
        if (gameModel.NextCriticalValue > 0)
        {
            actualClickIncrement += gameModel.NextCriticalValue;
            gameModel.NextCriticalValue = 0;
        }

        for (int i = 0; i < actualClickIncrement; i++)
        {
            audioManager?.PlayPiniataClickSound();
            bool isOpened = ctrl.HandleClick();
            ctrl.SpawnHitParticle();
            ctrl.BouncePiniata();

            if (isOpened)
            {
                HandleSpecialPiniata(ctrl);
                break;
            }
        }
    }

    private void HandleSpecialPiniata(PiniataController ctrl)
    {
        string lowerName = ctrl.gameObject.name.ToLower();
        if (lowerName.Contains("gold"))
        {
            goldenMultiplierActive = true;
            goldenTimerLeft = multiplierDuration;
            goldenMultiplierValue = 2f;
            gameView.ShowNotificationsWithTimer("Golden Piniata! Coins x2 for 15s!", 3f);

            RemovePiniata(ctrl, false);
        }
        else if (lowerName.Contains("black"))
        {
            gameModel.Timer = 0; 
            RemovePiniata(ctrl, false);
            StartCoroutine(WaitAndGameOver(GameOverPopupDelay));
        }
        else
        {
            PiniataFullyOpened(ctrl);
        }
    }

    private void PiniataFullyOpened(PiniataController ctrl)
    {
        int points = (int)(ctrl.RequiredClicks * goldenMultiplierValue);
        gameModel.Score += points;

        OnScoreUpdated?.Invoke(gameModel.Score);
        gameView.UpdateScore(gameModel.Score);

        destroyedPiniataCount++;
        gameView.UpdatePiniatasDestroyed(destroyedPiniataCount);
        audioManager?.PlayPiniataSmashSound();
        
        if (destroyedPiniataCount % 15 == 0)
        {
            gameModel.Timer += 15f;
            gameView.ShowNotificationsWithTimer("+15s bonus time!", 3f);
        }
        
        int xp = PlayerProgression.Instance.CalculateXP(ctrl.RequiredClicks);
        PlayerProgression.Instance.AddXP(xp);

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
        if (destroyedByUser && piniataSmashParticle != null)
        {
            Instantiate(piniataSmashParticle, ctrl.transform.position, Quaternion.identity);
        }
        Destroy(ctrl.gameObject);
    }

    //====================
    //  GOLDEN MULTIPLIER
    //====================

    private void UpdateGoldenMultiplier(float deltaTime)
    {
        if (!goldenMultiplierActive) return;
        if (goldenTimerLeft > 0)
        {
            goldenTimerLeft -= deltaTime;
            if (goldenTimerLeft <= 0)
            {
                goldenMultiplierActive = false;
                goldenMultiplierValue = 1f;
                gameView.ShowNotificationsWithTimer("Golden Multiplier ended!", 3f);
            }
        }
    }

    //====================
    //  BLACK => GAMEOVER
    //====================

    private IEnumerator WaitAndGameOver(float delay)
    {
        yield return new WaitForSeconds(delay);
        TriggerGameOver();
    }

    private void TriggerGameOver()
    {
        if (isGameOver) return;
        isGameOver = true;
        
        foreach (var ctrl in activePiniatas)
        {
            Destroy(ctrl.gameObject);
        }
        activePiniatas.Clear();
        
        MainMenuManager.TryUpdateBestStats(
            gameModel.Score, 
            destroyedPiniataCount,
            bombsUsedInSession,
            criticalUsedInSession
        );
        
        gameView.ShowGameOverPopup(
            gameModel.Score,
            destroyedPiniataCount,
            bombsUsedInSession,
            criticalUsedInSession
        );
    }

    //====================
    //  BOMB / CRITICAL
    //====================

    private void TryGrantBomb()
    {
        if (piniatasOpenedSinceLastBomb >= 3)
        {
            float roll = UnityEngine.Random.value;
            if (roll <= 0.2f)
            {
                gameModel.BombCount++;
                OnBombCountUpdated?.Invoke(gameModel.BombCount);
                gameView.ShowNotificationsWithTimer("You've won a BOMB!", 3f);
                piniatasOpenedSinceLastBomb = 0;
            }
        }
    }

    public void OnBombButtonClicked()
    {
        if (isGameOver) return;
        if (gameModel.BombCount <= 0) return;

        vibrate.DefaultVibration();
        gameModel.BombCount--;
        bombsUsedInSession++;
        int totalXP = 0;

        OnBombCountUpdated?.Invoke(gameModel.BombCount);
        audioManager?.PlayBombSound();
        cameraShake.Shake(0.2f,0.3f);
        
        List<PiniataController> toRemove = new List<PiniataController>(activePiniatas);

        foreach (var ctrl in toRemove)
        {
            string lowerName = ctrl.gameObject.name.ToLower();

            // Check for golden / black
            if (lowerName.Contains("gold"))
            {
                goldenMultiplierActive = true;
                goldenTimerLeft = multiplierDuration;
                goldenMultiplierValue = 2f;
                gameView.ShowNotificationsWithTimer("Golden Piniata! Coins x2 for 15s!", 3f);
            }
            else if (lowerName.Contains("black"))
            {
                gameModel.Timer = 0;
                StartCoroutine(WaitAndGameOver(GameOverPopupDelay));
            }
            else
            {
                gameModel.Score += (int)(ctrl.RequiredClicks * goldenMultiplierValue);
                destroyedPiniataCount++;
                totalXP += PlayerProgression.Instance.CalculateXP(ctrl.RequiredClicks);
            }
            
            if (piniataSmashParticle != null)
            {
                Instantiate(piniataSmashParticle, ctrl.transform.position, Quaternion.identity);
            }
            activePiniatas.Remove(ctrl);
            Destroy(ctrl.gameObject);
        }
        
        if (totalXP > 0)
        {
            PlayerProgression.Instance.AddXP(totalXP);
        }

        OnScoreUpdated?.Invoke(gameModel.Score);
        gameView.UpdateScore(gameModel.Score);
        gameView.UpdatePiniatasDestroyed(destroyedPiniataCount);
    }

    private void TryGrantCritical()
    {
        if (piniatasOpenedSinceLastCritical >= 5)
        {
            float roll = UnityEngine.Random.value;
            if (roll <= 0.3f)
            {
                gameModel.CriticalCount++;
                OnCriticalCountUpdated?.Invoke(gameModel.CriticalCount);
                gameView.ShowNotificationsWithTimer("You've won a CRITICAL HIT!", 3f);
                piniatasOpenedSinceLastCritical = 0;
            }
        }
    }

    public void OnCriticalButtonClicked()
    {
        if (isGameOver) return;
        if (gameModel.CriticalCount <= 0) return;
        
        vibrate.DefaultVibration();
        gameModel.CriticalCount--;
        criticalUsedInSession++;
        int totalXP = 0;

        OnCriticalCountUpdated?.Invoke(gameModel.CriticalCount);
        audioManager?.PlayCriticalSound();
        cameraShake.Shake(0.2f,0.3f);

        int randomX = UnityEngine.Random.Range(1, 10);
        gameView.ShowNotificationsWithTimer($"All Piniatas reduced by {randomX} clicks!", 3f);
        
        List<PiniataController> toRemove = new List<PiniataController>(activePiniatas);

        foreach (var ctrl in toRemove)
        {
            string lowerName = ctrl.gameObject.name.ToLower();
            
            ctrl.RequiredClicks -= randomX;

            if (ctrl.RequiredClicks <= ctrl.CurrentClicks)
            {
                // if golden => multiplier, if black => immediate game over, else normal
                if (lowerName.Contains("gold"))
                {
                    goldenMultiplierActive = true;
                    goldenTimerLeft = multiplierDuration;
                    goldenMultiplierValue = 2f;
                    gameView.ShowNotificationsWithTimer("Golden Piniata! Coins x2 for 15s!", 3f);
                }
                else if (lowerName.Contains("black"))
                {
                    gameModel.Timer = 0;
                    StartCoroutine(WaitAndGameOver(GameOverPopupDelay));
                }
                else
                {
                    gameModel.Score += (int)((ctrl.RequiredClicks + randomX) * goldenMultiplierValue);
                    destroyedPiniataCount++;
                    totalXP += PlayerProgression.Instance.CalculateXP(ctrl.RequiredClicks);

                }
                
                if (piniataSmashParticle != null)
                {
                    Instantiate(piniataSmashParticle, ctrl.transform.position, Quaternion.identity);
                }
                activePiniatas.Remove(ctrl);
                Destroy(ctrl.gameObject);
            }
            else
            {
                ctrl.UpdateClicksText();
            }
        }
        
        if (totalXP > 0)
        {
            PlayerProgression.Instance.AddXP(totalXP);
        }
        
        gameView.UpdateScore(gameModel.Score);
        OnScoreUpdated?.Invoke(gameModel.Score);
        gameView.UpdatePiniatasDestroyed(destroyedPiniataCount);
    }

    #region Timer

    private void UpdateTimer(float deltaTime)
    {
        if (gameModel.Timer > 0 && !isGameOver)
        {
            gameModel.Timer -= deltaTime;
            gameView.UpdateTimer(gameModel.Timer);

            float elapsed = 120f - gameModel.Timer;
            if (elapsed >= nextSpeedIncreaseTime)
            {
                globalGravityScale += speedIncreaseAmount;
                nextSpeedIncreaseTime += 30f;

                gameView.ShowNotificationsWithTimer("Piniatas now fall faster!", 3f);

                foreach (var ctrl in activePiniatas)
                {
                    var rb = ctrl.GetComponent<Rigidbody2D>();
                    if (rb) rb.gravityScale = globalGravityScale;
                }
            }
            if (gameModel.Timer <= 0)
            {
                gameModel.Timer = 0;
                gameView.UpdateTimer(gameModel.Timer);
                StartCoroutine(WaitAndGameOver(1f));
            }
        }
    }

    #endregion

    #region Pause / Resume

    public void OnPauseButtonClicked()
    {
        if (isGameOver) return;
        Time.timeScale = 0f; 
        gameView.ShowPausePopup(true);
        
        foreach (var ctrl in activePiniatas)
        {
            ctrl.gameObject.SetActive(false);
        }

        audioManager?.PlayPiniataClickSound();
    }

    public void ResumeGame()
    {
        foreach (var ctrl in activePiniatas)
        {
            ctrl.gameObject.SetActive(true);
        }

        gameView.ShowPausePopup(false);
        progressionView?.SetVisible(true);
        Time.timeScale = 1f;
        audioManager?.PlayPiniataClickSound();
    }

    public void BackToMainMenuFromPause()
    {
        foreach (var ctrl in activePiniatas)
        {
            ctrl.gameObject.SetActive(true);
        }

        Time.timeScale = 1f;
        progressionView?.SetVisible(true);
        audioManager?.PlayPiniataClickSound();
        sceneLoader.LoadNextScene();
    }

    #endregion

    public void RestartGame()
    {
        foreach (var ctrl in activePiniatas)
        {
            ctrl.gameObject.SetActive(true);
        }
        Time.timeScale = 1f;
        sceneLoader.ReactivateCurrentScene(); 
    }

    public void BackToMainMenuFromGameOver()
    {
        foreach (var ctrl in activePiniatas)
        {
            ctrl.gameObject.SetActive(true);
        }
        Time.timeScale = 1f;
        sceneLoader.LoadNextScene();
    }
}
