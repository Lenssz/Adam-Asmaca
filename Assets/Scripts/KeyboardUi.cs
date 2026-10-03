using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Collections.Generic;
using Photon.Pun; // KÖPRÜ İÇİN EKLENDİ

public class KeyboardUI : MonoBehaviour
{
    [Header("Klavye Satırları - Her satır için bir Transform")]
    public Transform row1;
    public Transform row2;
    public Transform row3;

    [Header("Tuş Prefab")]
    public GameObject keyButtonPrefab;

    [Header("Renkler")]
    public Color defaultColor = new Color(0.25f, 0.25f, 0.35f);
    public Color correctColor = new Color(0.2f, 0.75f, 0.35f);
    public Color wrongColor = new Color(0.8f, 0.2f, 0.2f);
    public Color presentColor = new Color(0.85f, 0.75f, 0.1f);

    private Dictionary<char, Button> keyButtons = new Dictionary<char, Button>();
    private Dictionary<char, Image> keyImages = new Dictionary<char, Image>();

    private static readonly string[] rows = new string[]
    {
        "QWERTYUIOPĞÜ",
        "ASDFGHJKLŞİ",
        "ZXCVBNMÖÇ"
    };

    void Awake()
    {
        Transform[] rowTransforms = { row1, row2, row3 };

        for (int r = 0; r < rows.Length; r++)
        {
            foreach (char c in rows[r])
            {
                if (c == 'q') continue;

                GameObject btn = Instantiate(keyButtonPrefab, rowTransforms[r]);
                TMP_Text label = btn.GetComponentInChildren<TMP_Text>();
                Button button = btn.GetComponent<Button>();
                Image img = btn.GetComponent<Image>();

                label.text = c.ToString();
                img.color = defaultColor;

                char captured = c;
                button.onClick.AddListener(() => OnKeyPressed(captured));

                keyButtons[c] = button;
                keyImages[c] = img;
            }
        }
    }

    void OnEnable()
    {
        // Eğer GameManager varsa (ki ana menüden geldiyse vardır), tek oyunculu eventleri dinle
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnLetterGuessed += OnLetterGuessed;
            GameManager.Instance.OnWordChanged += ResetKeyboard;
        }
    }

    void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnLetterGuessed -= OnLetterGuessed;
            GameManager.Instance.OnWordChanged -= ResetKeyboard;
        }
    }

    void OnKeyPressed(char letter)
    {
        if (PaperPageTransition.IsTransitioning) return;
        // İŞTE KÖPRÜ BURASI!
        if (PhotonNetwork.InRoom)
        {
            // İnternetteysek harfi YENİ multiplayer patronuna fırlat
            MultiplayerGameManager multiManager = FindObjectOfType<MultiplayerGameManager>();
            if (multiManager != null)
            {
                multiManager.GuessLetter(letter);
            }
        }
        else
        {
            // Tek oyunculuysak ESKİ patrona fırlat
            if (GameManager.Instance != null)
                GameManager.Instance.GuessLetter(letter);
        }
    }

    // Multiplayer kodumuz da erişebilsin diye PUBLIC yaptık
    public void OnLetterGuessed(char letter, bool correct)
    {
        if (!keyImages.ContainsKey(letter)) return;

        Image img = keyImages[letter];
        Button btn = keyButtons[letter];
        Color target = correct ? correctColor : wrongColor;

        img.DOColor(target, 0.25f);

        btn.interactable = false;
    }

    public void ResetKeyboard()
    {
        foreach (var kvp in keyImages)
        {
            kvp.Value.DOColor(defaultColor, 0.15f);
        }
        foreach (var kvp in keyButtons)
        {
            kvp.Value.interactable = true;
            kvp.Value.transform.localScale = Vector3.one;
        }
    }
}
