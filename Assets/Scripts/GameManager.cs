using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Kelime Veritabanı")]
    public WordDatabase wordDatabase;

    [Header("Max Yanlış Tahmin")]
    public int maxWrongGuesses = 6;
    


    private int selectedCategoryIndex = -1;

    // RAM'deki geçici torbamız
    private Dictionary<int, List<string>> categoryPools = new Dictionary<int, List<string>>();

    public string CurrentWord { get; private set; }
    public HashSet<char> GuessedLetters { get; private set; } = new HashSet<char>();
    public int WrongGuessCount { get; private set; }
    public bool IsGameOver { get; private set; }
    public bool IsWon { get; private set; }

    public System.Action<char, bool> OnLetterGuessed;
    public System.Action OnWordChanged;
    public System.Action<bool> OnGameEnd;
    
    void Awake()
    {
        // GÜÇLENDİRİLMİŞ SİNGLETON: Klonların eventleri tetiklemesini %100 engeller
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnEnable()
    {
        if (Instance == this) // Sadece asıl yönetici sahne geçişlerini dinlesin
            SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        if (Instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "GAMESCENE")
        {
            StartGame();
        }
    }

    public void SelectCategory(int index)
    {
        if (PaperPageTransition.IsTransitioning) return;
        selectedCategoryIndex = index;
        PaperPageTransition.LoadScene("GAMESCENE");
    }

    public void StartGame()
    {
        if (selectedCategoryIndex == -1)
        {
            Debug.LogWarning("Kategori seçilmemiş! Varsayılan olarak LoL (0) seçiliyor.");
            selectedCategoryIndex = 0;
        }

        LoadNewWord();
    }
    

    public void LoadNewWord()
    {
        if (wordDatabase == null) return;

        List<string> fullList = GetCurrentWordList();
        if (fullList == null || fullList.Count == 0) return;

        // --- KALICI KAYIT (SAVE) SİSTEMLİ HAVUZ ---
        string saveKey = "CategoryPool_" + selectedCategoryIndex;

        // Torba henüz RAM'e yüklenmediyse, Cihazın Hafızasından (PlayerPrefs) çekmeyi dene
        if (!categoryPools.ContainsKey(selectedCategoryIndex))
        {
            if (PlayerPrefs.HasKey(saveKey))
            {
                string savedData = PlayerPrefs.GetString(saveKey);
                if (!string.IsNullOrEmpty(savedData))
                {
                    // Virgüllerle ayırıp listeye çevir
                    categoryPools[selectedCategoryIndex] = savedData.Split(',').ToList();
                    Debug.Log("Kayıtlı torba yüklendi!");
                }
                else
                {
                    categoryPools[selectedCategoryIndex] = new List<string>(fullList);
                }
            }
            else
            {
                // İlk defa oynanıyorsa torbayı sıfırdan doldur
                categoryPools[selectedCategoryIndex] = new List<string>(fullList);
            }
        }

        List<string> currentPool = categoryPools[selectedCategoryIndex];

        // Eğer torba TAMAMEN BOŞALDIYSA (Kategori bittiyse), kelimeleri baştan doldur
        if (currentPool.Count == 0)
        {
            currentPool = new List<string>(fullList);
            Debug.Log("Torba bitmişti, yeniden dolduruldu!");
        }

        // Kelimeyi seç ve torbadan at
        int randomIndex = Random.Range(0, currentPool.Count);
        string chosenWord = currentPool[randomIndex];
        currentPool.RemoveAt(randomIndex);

        // TORBANIN YENİ HALİNİ CİHAZA KAYDET (Oyunu kapatsan da silinmez)
        PlayerPrefs.SetString(saveKey, string.Join(",", currentPool));
        PlayerPrefs.Save();

        CurrentWord = chosenWord.ToUpper(new System.Globalization.CultureInfo("tr-TR"));
        Debug.Log("BAŞARILI! Seçilen Kelime: " + CurrentWord + " | Torbada Kalan Kelime Sayısı: " + currentPool.Count);

        GuessedLetters.Clear();
        WrongGuessCount = 0;
        IsGameOver = false;
        IsWon = false;

        OnWordChanged?.Invoke();
        
    }
    

    List<string> GetCurrentWordList()
    {
        if (wordDatabase == null) return null;
        switch (selectedCategoryIndex)
        {
            case 0: return wordDatabase.lolWords;
            case 1: return wordDatabase.valorantWords;
            case 2: return wordDatabase.csWords;
            case 3: return wordDatabase.generalWords;
            case 4: return wordDatabase.countryWords;
            case 5: return wordDatabase.minecraftWords;
            default: return null;
        }
    }

    public void GuessLetter(char letter)
    {
        if (PhotonNetwork.InRoom) return;
        if (IsGameOver) return;
        letter = char.ToUpper(letter, new System.Globalization.CultureInfo("tr-TR"));
        if (GuessedLetters.Contains(letter)) return;

        GuessedLetters.Add(letter);
        bool correct = CurrentWord.Contains(letter);
        if (!correct) WrongGuessCount++;

        OnLetterGuessed?.Invoke(letter, correct);

        bool allRevealed = CurrentWord.All(c => GuessedLetters.Contains(c));
        if (allRevealed)
        {
            IsWon = true;
            IsGameOver = true;
            EconomyManager.AddCoin(1);
            OnGameEnd?.Invoke(true);
        }
        else if (WrongGuessCount >= maxWrongGuesses)
        {
            IsGameOver = true;
            OnGameEnd?.Invoke(false);
        }
    }

    public void GoToMainMenu()
    {
        PaperPageTransition.LoadScene("SampleScene");
    }
    public int CategoryIndex { get { return selectedCategoryIndex; } }
}
