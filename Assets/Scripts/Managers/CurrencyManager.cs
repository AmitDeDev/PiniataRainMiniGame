using UnityEngine;
using System;

public class CurrencyManager : MonoBehaviour
{
    public static CurrencyManager Instance { get; private set; }

    private const string COINS_KEY = "PlayerCoins";
    private int currentCoins = 0;

    public event Action<int> OnCoinsChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadCoins();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void LoadCoins()
    {
        currentCoins = PlayerPrefs.GetInt(COINS_KEY, 0);
        OnCoinsChanged?.Invoke(currentCoins);
    }

    private void SaveCoins()
    {
        PlayerPrefs.SetInt(COINS_KEY, currentCoins);
        PlayerPrefs.Save();
    }

    public void AddCoins(int amount)
    {
        if (amount <= 0) return;
        currentCoins += amount;
        SaveCoins();
        OnCoinsChanged?.Invoke(currentCoins);
    }

    public bool TrySpendCoins(int amount)
    {
        if (currentCoins < amount)
            return false;

        currentCoins -= amount;
        SaveCoins();
        OnCoinsChanged?.Invoke(currentCoins);
        return true;
    }

    public int GetBalance() => currentCoins;
}