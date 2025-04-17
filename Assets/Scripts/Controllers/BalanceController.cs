using UnityEngine;
using System;

public class BalanceController : MonoBehaviour
{
    public static BalanceController Instance { get; private set; }

    private const string BALANCE_KEY = "PlayerCoins";
    private int balance;

    public event Action<int> OnBalanceChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadBalance();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void LoadBalance()
    {
        balance = PlayerPrefs.GetInt(BALANCE_KEY, 0);
        OnBalanceChanged?.Invoke(balance);
    }

    private void SaveBalance()
    {
        PlayerPrefs.SetInt(BALANCE_KEY, balance);
        PlayerPrefs.Save();
    }

    public int GetBalance() => balance;

    public void AddCoins(int amount)
    {
        if (amount <= 0) return;
        balance += amount;
        SaveBalance();
        OnBalanceChanged?.Invoke(balance);
    }

    public bool TrySpendCoins(int amount)
    {
        if (balance < amount)
            return false;

        balance -= amount;
        SaveBalance();
        OnBalanceChanged?.Invoke(balance);
        return true;
    }
}