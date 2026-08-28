using UnityEngine;
using TMPro;

public class CoinDisplay : MonoBehaviour
{
    [Header("Arayüz Bağlantısı")]
    public TMP_Text coinText;

    // Obje sahnede ilk oluştuğunda çalışır
    void Start()
    {
        UpdateCoinDisplay();
    }

    // Obje (veya panel) her aktif edildiğinde çalışır (Ana menüye geri dönüldüğünde güncellenmesi için çok önemli)
    void OnEnable()
    {
        UpdateCoinDisplay();
    }

    // Parayı çekip ekrana yazdıran fonksiyon
    public void UpdateCoinDisplay()
    {
        if (coinText != null)
        {
            // EconomyManager'dan parayı çekiyoruz
            int currentCoins = EconomyManager.GetCoins();

            // Ekrana yazdırıyoruz. İstersen yanına altın emojisi de koyabilirsin.
            coinText.text = "Altın : " + currentCoins;

            // Alternatif kullanım: coinText.text = currentCoins + " Altın";
        }
    }
}
