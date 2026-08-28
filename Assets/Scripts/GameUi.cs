using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class GameUI : MonoBehaviour
{
    [Header("Sonuç Paneli")]
    public GameObject resultPanel;
    public TMP_Text resultTitle;
    public TMP_Text resultWord;
    public TMP_Text coinWord;
    public Button playAgainButton;
    public Button backMenuButton;
    [Header("Kapatılacak Ekranlar")]
    public GameObject hintPanel;


    [Header("Renkler")]
    public Color winColor = new Color(0.2f, 0.9f, 0.4f);
    public Color loseColor = new Color(0.9f, 0.3f, 0.3f);

    [Header("Referanslar")]
    public HangmanDrawer hangmanDrawer;   // RawImage objesini sürükle
    public WordDisplay wordDisplay;

    void OnEnable()
    {
        GameManager.Instance.OnGameEnd += ShowResult;
    }

    void OnDisable()
    {
        if (GameManager.Instance == null) return;
        GameManager.Instance.OnGameEnd -= ShowResult;
    }

    void Start()
    {
        resultPanel.SetActive(false);

        playAgainButton.onClick.AddListener(() =>
        {
            resultPanel.SetActive(false);
            GameManager.Instance.LoadNewWord();
        });

        backMenuButton.onClick.AddListener(() =>
        {
            resultPanel.SetActive(false);
            GameManager.Instance.GoToMainMenu();
        });
    }

    void ShowResult(bool won)
    {
        if (hintPanel != null)
        {
            hintPanel.SetActive(false);
        }
        if (!won)
        {
            wordDisplay.RevealAllLetters();
            hangmanDrawer.PlayLoseEffect();
        }

        DOVirtual.DelayedCall(0.8f, () =>
        {
            resultPanel.SetActive(true);
            resultPanel.transform.localScale = Vector3.zero;
            resultPanel.transform.DOScale(1f, 0.4f).SetEase(Ease.OutBack);

            resultTitle.text = won ? "Kazandın!" : "Kaybettin!";
            resultTitle.color = won ? winColor : loseColor;
            resultWord.text = (won ? "" : "Kelime: ") + GameManager.Instance.CurrentWord;
            if (won)
            {
                int currentCoin = EconomyManager.GetCoins();
                coinWord.text = "Kazanılan Altın: 1 \nMevcut Altın: " + currentCoin;
            }
            else
            {
                int currentCoin = EconomyManager.GetCoins();
                coinWord.text = "Mevcut Altın: " + currentCoin;
            }
        });
    }
}