using UnityEngine;

public static class EconomyManager
{
    // Mevcut parayı getirir
    public static int GetCoins()
    {
        return PlayerPrefs.GetInt("PlayerCoins", 0);
    }

    // Para ekler
    public static void AddCoin(int amount)
    {
        int current = GetCoins();
        PlayerPrefs.SetInt("PlayerCoins", current + amount);
        PlayerPrefs.Save();
    }

    // Para harcar (Eğer yeterli para varsa harcar ve true döner)
    public static bool SpendCoin(int amount)
    {
        int current = GetCoins();
        if (current >= amount)
        {
            PlayerPrefs.SetInt("PlayerCoins", current - amount);
            PlayerPrefs.Save();
            return true;
        }
        return false;
    }
}