using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using UnityEngine.UI;

public class AIHintManager : MonoBehaviour
{
    [Header("UI Elemanları")]
    public GameObject hintPanel; // Arka planı ve yazıyı tutan ana obje
    public TMP_Text hintText;

    [Header("API Ayarları")]
    private string[] apiKeys = System.Array.Empty<string>();

    [System.Serializable]
    private class LocalGeminiSettings { public string[] apiKeys = System.Array.Empty<string>(); }


    private string apiUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key=";
    public int hintCost = 3;
    private string lastFetchedWord = ""; // Sürekli istek atmamak için hafıza
    private int currentKeyIndex = 0; // O an aktif olarak kullanılan anahtarın sırası

    void Start()
    {
        // This optional asset stays on the developer's machine and is ignored by Git.
        var localSettings = Resources.Load<TextAsset>("LocalSecrets/GeminiSettings");
        if (localSettings != null)
        {
            var settings = JsonUtility.FromJson<LocalGeminiSettings>(localSettings.text);
            if (settings != null && settings.apiKeys != null)
                apiKeys = System.Array.FindAll(settings.apiKeys, key => !string.IsNullOrWhiteSpace(key));
        }

        // Oyun başladığında ipucu paneli kapalı olsun
        if (hintPanel != null)
        {
            hintPanel.SetActive(false);
        }
    }

    public void ToggleHintPanel()
    {
        if (hintPanel == null) return;

        bool willBeOpen = !hintPanel.activeSelf;
        hintPanel.SetActive(willBeOpen);

        if (willBeOpen)
        {
            // NOT: Eğer Multiplayer'dayken farklı bir GameManager kullanıyorsan
            // ileride buraya "if (isMultiplayer)" gibi bir kontrol ekleyip 
            // kelimeyi MultiplayerManager'dan çekecek şekilde güncelleyebilirsin.
            string currentSecretWord = GameManager.Instance.CurrentWord;
            int categoryIndex = GameManager.Instance.CategoryIndex;
            string aiCategoryName = "Genel";

            switch (categoryIndex)
            {
                case 0:
                    aiCategoryName = "League of Legends Oyun Terimleri ve Şampiyonları";
                    break;
                case 1:
                    aiCategoryName = "Valorant Oyunu";
                    break;
                case 2:
                    aiCategoryName = "CS:GO (Counter-Strike) Oyunu";
                    break;
                case 3:
                    aiCategoryName = "Günlük Hayat ve Genel Kültür";
                    break;
                case 4:
                    aiCategoryName = "Ülkeler ve Coğrafya";
                    break;
                case 5:
                    aiCategoryName = "Minecraft Oyunu";
                    break;
            }

            if (!string.IsNullOrEmpty(currentSecretWord) && currentSecretWord != lastFetchedWord)
            {
                // ---- VİP KAPI KONTROLÜ BAŞLIYOR ----
                if (EconomyManager.GetCoins() >= hintCost)
                {
                    // Oyuncunun parası var! İşleme devam et ama PARAYI HENÜZ KESME!
                    lastFetchedWord = currentSecretWord;
                    StartCoroutine(FetchHintFromGemini(currentSecretWord, aiCategoryName, 0));
                }
                else
                {
                    // Parası yetmiyor, API'ye hiç istek atmadan uyarı ver
                    hintText.text = $"Yetersiz Altın!\nİpucu almak için {hintCost} altına ihtiyacın var.\nMevcut Altının: {EconomyManager.GetCoins()}";
                }
                // ---- KONTROL BİTTİ ----
            }
        }
    }

    // Fonksiyona kaç kere tekrar denediğimizi tutan 'retryCount' parametresini ekledik
    IEnumerator FetchHintFromGemini(string word, string category, int retryCount)
    {
        // Güvenlik kontrolü: Eğer Unity Editöründen API girilmemişse uyar
        if (apiKeys == null || apiKeys.Length == 0)
        {
            hintText.text = "Sistemde API Anahtarı bulunamadı!";
            yield break;
        }

        hintText.text = "Yapay zeka analiz ediyor...";

        string prompt = $"Sen bir adam asmaca oyunu ipucu motorusun. Kategori: '{category}'. Gizli Kelime: '{word}'. Lütfen kelimenin anlamını kesinlikle bu KATEGORİ bağlamında düşün. Bu kelimeyi ASLA cümlenin içinde kullanmadan, en fazla 8 kelimelik tek bir cümle kur. SADECE ipucunu metin olarak ver.";
        string jsonData = "{\"contents\":[{\"parts\":[{\"text\":\"" + prompt + "\"}]}]}";

        // Aktif olan API anahtarını kullanarak istek atıyoruz
        using (UnityWebRequest request = new UnityWebRequest(apiUrl + apiKeys[currentKeyIndex], "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                EconomyManager.SpendCoin(hintCost);
                string response = request.downloadHandler.text;
                string extractedHint = ExtractTextFromJson(response);
                hintText.text = "💡 " + extractedHint;
            }
            else
            {
                // 429 hatası kotanın (limitin) dolduğunu gösterir
                if (request.responseCode == 429)
                {
                    Debug.LogWarning($"[AIHintManager] {currentKeyIndex}. anahtarın limiti doldu.");

                    // Eğer elindeki tüm anahtarları henüz denemediysen
                    if (retryCount < apiKeys.Length - 1)
                    {
                        // Bir sonraki anahtara geç (Dizinin sonuna gelirse başa döner)
                        currentKeyIndex = (currentKeyIndex + 1) % apiKeys.Length;
                        Debug.Log($"[AIHintManager] {currentKeyIndex}. yedek anahtara geçiliyor ve tekrar deneniyor...");

                        // Oyuncuya hiçbir hata göstermeden, yeni anahtarla işlemi baştan başlat
                        yield return StartCoroutine(FetchHintFromGemini(word, category, retryCount + 1));
                    }
                    else
                    {
                        // Bütün anahtarlar tükendiyse
                        hintText.text = "Günlük ipucu limiti tamamen doldu.";
                        lastFetchedWord = "";
                    }
                }
                else
                {
                    hintText.text = "Sisteme bağlanılamadı.";
                    Debug.LogError("API Hatası: " + request.error);
                    Debug.LogError("Detay: " + request.downloadHandler.text);
                    lastFetchedWord = "";
                }
            }
        }
    }

    private string ExtractTextFromJson(string json)
    {
        try
        {
            int startIndex = json.IndexOf("\"text\": \"") + 9;
            int endIndex = json.IndexOf("\"", startIndex);
            string rawText = json.Substring(startIndex, endIndex - startIndex);
            return rawText.Replace("\\n", "").Trim();
        }
        catch
        {
            return "Sistem bir yanıt bulamadı...";
        }
    }
}
