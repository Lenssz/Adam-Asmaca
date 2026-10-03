using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Collections;

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
    private Coroutine resultRoutine;

    void OnEnable()
    {
        GameManager.Instance.OnGameEnd += ShowResult;
    }

    void OnDisable()
    {
        if (resultRoutine != null) { StopCoroutine(resultRoutine); resultRoutine = null; }
        if (GameManager.Instance == null) return;
        GameManager.Instance.OnGameEnd -= ShowResult;
    }

    void Start()
    {
        resultPanel.SetActive(false);

        playAgainButton.onClick.AddListener(() =>
        {
            if (PaperPageTransition.IsTransitioning) return;
            resultPanel.SetActive(false);
            GameManager.Instance.LoadNewWord();
        });

        backMenuButton.onClick.AddListener(() =>
        {
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

        if (resultRoutine != null) StopCoroutine(resultRoutine);
        resultRoutine = StartCoroutine(ShowResultAfterDrawing(won));
    }

    IEnumerator ShowResultAfterDrawing(bool won)
    {
        int revision = hangmanDrawer != null ? hangmanDrawer.DrawingRevision : 0;
        string word = GameManager.Instance.CurrentWord;
        // Keep the existing minimum delay, then let pending pencil strokes finish.
        yield return new WaitForSeconds(0.8f);
        if (hangmanDrawer != null) yield return hangmanDrawer.WaitForDrawing();
        if (hangmanDrawer != null && revision != hangmanDrawer.DrawingRevision) { resultRoutine = null; yield break; }
        {
            resultPanel.SetActive(true);
            resultPanel.transform.localScale = Vector3.zero;
            resultPanel.transform.DOScale(1f, 0.4f).SetEase(Ease.OutBack);

            resultTitle.text = won ? "Kazandın!" : "Kaybettin!";
            resultTitle.color = won ? winColor : loseColor;
            resultWord.text = (won ? "" : "Kelime: ") + word;
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
        }
        resultRoutine = null;
    }
}
