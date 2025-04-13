using UnityEngine;
using System;

public class PlayerProgression : MonoBehaviour
{
    public static PlayerProgression Instance { get; private set; }

    [Header("XP Settings")]
    [SerializeField] private int baseXPPerLevel = 100;
    [SerializeField] private float difficultyCurve = 1.3f;
    [SerializeField] private int baseXPPerPiniata = 10;
    [SerializeField] private float xpMultiplierPerClick = 0.25f;

    private ProgressionData data;

    public event Action<int> OnLevelChanged;
    public event Action<int, int> OnXPChanged; // currentXP, requiredXP

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadProgression();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public int CurrentLevel => data.Level;
    public int CurrentXP => data.CurrentXP;
    public int XPToNextLevel => Mathf.RoundToInt(baseXPPerLevel * Mathf.Pow(data.Level, difficultyCurve));

    public void AddXP(int amount)
    {
        data.CurrentXP += amount;

        while (data.CurrentXP >= XPToNextLevel)
        {
            data.CurrentXP -= XPToNextLevel;
            data.Level++;
            OnLevelChanged?.Invoke(data.Level);
        }

        OnXPChanged?.Invoke(data.CurrentXP, XPToNextLevel);
        SaveProgression();
    }
    
    public int CalculateXP(int requiredClicks)
    {
        float xp = baseXPPerPiniata + (requiredClicks * xpMultiplierPerClick);
        return Mathf.RoundToInt(xp);
    }

    private void LoadProgression()
    {
        data = new ProgressionData
        {
            Level = PlayerPrefs.GetInt("PlayerLevel", 1),
            CurrentXP = PlayerPrefs.GetInt("PlayerXP", 0)
        };
    }

    private void SaveProgression()
    {
        PlayerPrefs.SetInt("PlayerLevel", data.Level);
        PlayerPrefs.SetInt("PlayerXP", data.CurrentXP);
        PlayerPrefs.Save();
    }
}