using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Collections.Generic;

public class WordDisplay : MonoBehaviour
{
    [Header("Referanslar")]
    public Transform letterContainer;   // Horizontal Layout Group buraya
    public GameObject letterSlotPrefab; // İçinde Image + TMP_Text olan prefab

    [Header("Animasyon")]
    public float slotRevealDelay = 0.08f;   // kareler arası gecikme
    public float slotRevealDuration = 0.3f;
    public float letterPlaceDuration = 0.25f;
    public Color correctLetterColor = new Color(0.2f, 0.9f, 0.4f);
    public Color hiddenSlotColor = new Color(0.15f, 0.15f, 0.25f);

    private List<TMP_Text> letterTexts = new List<TMP_Text>();
    private List<Image> slotImages = new List<Image>();
    void Start()
    {
        // Sahne açıldığında halihazırda seçilmiş bir kelime varsa kutuları hemen çiz!
        if (GameManager.Instance != null && !string.IsNullOrEmpty(GameManager.Instance.CurrentWord))
        {
            BuildWordDisplay();
        }
    }

    void OnEnable()
    {
        GameManager.Instance.OnWordChanged += BuildWordDisplay;
        GameManager.Instance.OnLetterGuessed += RevealLetters;
    }

    void OnDisable()
    {
        if (GameManager.Instance == null) return;
        GameManager.Instance.OnWordChanged -= BuildWordDisplay;
        GameManager.Instance.OnLetterGuessed -= RevealLetters;
    }

    void BuildWordDisplay()
    {
        // Temizle
        foreach (Transform child in letterContainer)
            Destroy(child.gameObject);
        letterTexts.Clear();
        slotImages.Clear();

        string word = GameManager.Instance.CurrentWord;

        for (int i = 0; i < word.Length; i++)
        {
            GameObject slot = Instantiate(letterSlotPrefab, letterContainer);
            TMP_Text txt = slot.GetComponentInChildren<TMP_Text>();
            Image img = slot.GetComponent<Image>();

            txt.text = "";
            img.color = hiddenSlotColor;

            letterTexts.Add(txt);
            slotImages.Add(img);

            // Animasyonlu açılış: scale 0 → 1, sırayla
            slot.transform.localScale = Vector3.zero;
            int idx = i;
            DOVirtual.DelayedCall(idx * slotRevealDelay, () =>
            {
                slot.transform.DOScale(1f, slotRevealDuration).SetEase(Ease.OutBack);
            });
        }
    }

    void RevealLetters(char letter, bool correct)
    {
        if (!correct) return;
        string word = GameManager.Instance.CurrentWord;

        for (int i = 0; i < word.Length; i++)
        {
            if (word[i] == letter)
            {
                int idx = i;
                TMP_Text txt = letterTexts[idx];
                Image img = slotImages[idx];

                // Pop animasyonu + renk değişimi
                txt.transform.localScale = Vector3.zero;
                txt.text = letter.ToString();

                Sequence seq = DOTween.Sequence();
                seq.Append(img.DOColor(correctLetterColor, 0.2f));
                seq.Join(txt.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack));
                seq.Append(txt.transform.DOPunchScale(Vector3.one * 0.3f, 0.2f));
            }
        }
    }

    // Oyun bitince tüm kelimeyi göster (kaybedilirse)
    public void RevealAllLetters()
    {
        string word = GameManager.Instance.CurrentWord;
        for (int i = 0; i < word.Length; i++)
        {
            if (letterTexts[i].text == "")
            {
                letterTexts[i].text = word[i].ToString();
                slotImages[i].DOColor(new Color(0.9f, 0.3f, 0.3f), 0.3f);
                letterTexts[i].transform.DOPunchScale(Vector3.one * 0.2f, 0.3f);
            }
        }
    }
}
