using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;

public class MultiplayerGameManager : MonoBehaviourPunCallbacks
{
    [Header("Oyun Ayarları")]
    public int maxWrongGuesses = 6;
    [Header("Veritabanı ve UI")]
    public WordDatabase wordDatabase;
    public Transform myWordContainer;
    public Transform opponentWordContainer;
    public GameObject myBoxPrefab;
    public GameObject opponentBoxPrefab;

    [Header("Bitiş Ekranı UI")]
    public GameObject winLossPanel;
    public TMP_Text resultText;
    public TMP_Text opponentNameText;
    public Button rematchButton; // Tekrar oyna düğmesi
    public TMP_Text rematchButtonText; // Düğmenin üzerindeki yazı

    [Header("Animasyon ve Renkler")]
    public float slotRevealDelay = 0.08f;
    public float slotRevealDuration = 0.3f;
    public Color correctLetterColor = new Color(0.2f, 0.9f, 0.4f);
    public Color hiddenSlotColor = new Color(0.15f, 0.15f, 0.25f);

    private string secretWord;
    private List<GameObject> myBoxes = new List<GameObject>();
    private List<GameObject> opponentBoxes = new List<GameObject>();
    private HashSet<char> guessedLetters = new HashSet<char>();
    private int myWrongGuessCount = 0;
    private bool isGameOver = false;
    private bool amIDead = false;
    private bool isOpponentDead = false;

    // Rematch Değişkenleri
    private bool iWantRematch = false;
    private bool opponentWantsRematch = false;

    void Start()
    {
        if (winLossPanel != null) winLossPanel.SetActive(false);
        StartNewRound();
    }

    void StartNewRound()
    {
        // Temizlik: Eski kutuları sil
        foreach (GameObject go in myBoxes) Destroy(go);
        foreach (GameObject go in opponentBoxes) Destroy(go);
        myBoxes.Clear();
        opponentBoxes.Clear();
        guessedLetters.Clear();
        myWrongGuessCount = 0;
        isGameOver = false;
        amIDead = false;
        isOpponentDead = false;
        iWantRematch = false;
        opponentWantsRematch = false;

        // Klavye ve Adam Asmaca'yı sıfırla
        KeyboardUI keyboard = FindFirstObjectByType<KeyboardUI>();
        if (keyboard != null) keyboard.ResetKeyboard();

        HangmanDrawer drawer = FindFirstObjectByType<HangmanDrawer>();
        if (drawer != null) drawer.ResetDrawing();

        if (winLossPanel != null) winLossPanel.SetActive(false);

        if (PhotonNetwork.IsMasterClient)
        {
            SendNewWord();
        }
    }

    void SendNewWord()
    {
        int categoryIndex = 0;
        if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("C"))
            categoryIndex = (int)PhotonNetwork.CurrentRoom.CustomProperties["C"];

        List<string> wordList = GetWordList(categoryIndex);
        string chosenWord = wordList[Random.Range(0, wordList.Count)].ToUpper(new System.Globalization.CultureInfo("tr-TR"));
        photonView.RPC("RPC_ReceiveWord", RpcTarget.All, chosenWord);
    }

    [PunRPC]
    void RPC_ReceiveWord(string word)
    {
        secretWord = word;
        if (opponentNameText != null && PhotonNetwork.PlayerListOthers.Length > 0)
            opponentNameText.text = "Rakip: " + PhotonNetwork.PlayerListOthers[0].NickName;

        for (int i = 0; i < secretWord.Length; i++)
        {
            GameObject myNewBox = Instantiate(myBoxPrefab, myWordContainer);
            myNewBox.GetComponentInChildren<TMP_Text>().text = "";
            myNewBox.GetComponent<Image>().color = hiddenSlotColor;
            myBoxes.Add(myNewBox);
            myNewBox.transform.localScale = PaperPageTransition.IsTransitioning ? Vector3.one : Vector3.zero;
            int idx = i;
            if (!PaperPageTransition.IsTransitioning) DOVirtual.DelayedCall(idx * slotRevealDelay, () => myNewBox.transform.DOScale(1f, slotRevealDuration).SetEase(Ease.OutBack).SetLink(myNewBox)).SetLink(myNewBox);

            GameObject opponentNewBox = Instantiate(opponentBoxPrefab, opponentWordContainer);
            opponentNewBox.GetComponent<Image>().color = hiddenSlotColor;
            opponentBoxes.Add(opponentNewBox);
            opponentNewBox.transform.localScale = PaperPageTransition.IsTransitioning ? Vector3.one : Vector3.zero;
            if (!PaperPageTransition.IsTransitioning) DOVirtual.DelayedCall(idx * slotRevealDelay, () => opponentNewBox.transform.DOScale(1f, slotRevealDuration).SetEase(Ease.OutBack).SetLink(opponentNewBox)).SetLink(opponentNewBox);
        }
    }

    public void GuessLetter(char letter)
    {
        if (isGameOver || amIDead) return;
        letter = char.ToUpper(letter, new System.Globalization.CultureInfo("tr-TR"));
        if (guessedLetters.Contains(letter)) return;
        guessedLetters.Add(letter);

        bool isCorrect = secretWord.Contains(letter);
        FindFirstObjectByType<KeyboardUI>()?.OnLetterGuessed(letter, isCorrect);

        if (isCorrect)
        {
            List<int> foundIndices = new List<int>();
            for (int i = 0; i < secretWord.Length; i++)
            {
                if (secretWord[i] == letter)
                {
                    foundIndices.Add(i);
                    TMP_Text txt = myBoxes[i].GetComponentInChildren<TMP_Text>();
                    txt.text = letter.ToString();
                    myBoxes[i].GetComponent<Image>().DOColor(correctLetterColor, 0.2f);
                    txt.transform.localScale = Vector3.zero;
                    txt.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack);
                }
            }
            photonView.RPC("RPC_OpponentGuessedCorrect", RpcTarget.Others, foundIndices.ToArray());

            if (myBoxes.All(box => box.GetComponentInChildren<TMP_Text>().text.Length > 0))
            {
                photonView.RPC("RPC_GameOver", RpcTarget.All, PhotonNetwork.LocalPlayer.ActorNumber);
            }
        }
        else
        {
            myWrongGuessCount++;
            HangmanDrawer drawer = FindFirstObjectByType<HangmanDrawer>();
            drawer?.AddPartToQueue(myWrongGuessCount);
            if (myWrongGuessCount >= maxWrongGuesses)
            {
                amIDead = true;
                drawer?.PlayLoseEffect();
                photonView.RPC("RPC_PlayerHanged", RpcTarget.Others);
                CheckDrawCondition();
            }
        }
    }

    [PunRPC]
    public void RPC_GameOver(int winnerActorNumber) // Mutlaka 'public' ve parametre 'int' olmalı
    {
        isGameOver = true;

        if (winLossPanel == null) return; // Güvenlik kontrolü

        winLossPanel.SetActive(true);
        rematchButton.interactable = true;
        rematchButtonText.text = "Tekrar Oyna";

        string resultMsg = "";

        // 1. BERABERE DURUMU (-1 gelirse)
        if (winnerActorNumber == -1)
        {

            resultMsg = "<color=#716844>BERABERE!</color>\n<size=80%>İkiniz de asıldınız...</size>";
        }
        // 2. KAZANMA DURUMU (ActorNumber eşleşirse)
        else if (winnerActorNumber == PhotonNetwork.LocalPlayer.ActorNumber)
        {
            EconomyManager.AddCoin(2);
            int currentCoins = EconomyManager.GetCoins();
            resultMsg = "<color=#456E42><size=120%>TEBRİKLER, KAZANDIN!</size></color>\n<size=80%>Kazanılan Altın 2</size>\n<size=80%>Mevcut Altın: " + currentCoins + "</size>";
        }
        // 3. KAYBETME DURUMU
        else
        {
            string winnerName = "Rakip";
            // Oyuncuyu ID ile odadan bulalım
            if (PhotonNetwork.CurrentRoom != null)
            {
                var winner = PhotonNetwork.CurrentRoom.GetPlayer(winnerActorNumber);
                if (winner != null) winnerName = winner.NickName;
            }
            EconomyManager.AddCoin(1);
            int currentCoins = EconomyManager.GetCoins();
            resultMsg = "<color=#914A42><size=120%>MAALESEF, KAYBETTİN!</size></color>\n<size=80%>Kazanan: " + winnerName + "</size>\n<size=80%>Kazanılan Altın 1</size>\n<size=80%>Mevcut Altın: " + currentCoins + "</size>";
        }

        // SONUÇ VE DOĞRU KELİME
        resultText.text = resultMsg + "\n\n<color=#716844>Doğru Kelime: " + secretWord + "</color>";

        // PANEL ANİMASYONU
        winLossPanel.transform.localScale = Vector3.zero;
        winLossPanel.transform.DOScale(1f, 0.5f).SetEase(Ease.OutBack).SetUpdate(true);
    }

    // --- REMATCH (TEKRAR OYNA) MANTIĞI ---

    public void OnRematchButtonClicked()
    {
        if (PaperPageTransition.IsTransitioning) return;
        iWantRematch = true;
        rematchButton.interactable = false;
        rematchButtonText.text = "Rakip Bekleniyor...";
        photonView.RPC("RPC_RequestRematch", RpcTarget.Others);
        CheckRematch();
    }

    [PunRPC]
    void RPC_RequestRematch()
    {
        opponentWantsRematch = true;
        CheckRematch();
    }

    void CheckRematch()
    {
        if (iWantRematch && opponentWantsRematch)
        {
            if (PhotonNetwork.IsMasterClient)
                photonView.RPC("RPC_StartNewRound", RpcTarget.All);
        }
    }

    [PunRPC]
    void RPC_StartNewRound() => StartNewRound();

    public void LeaveToMenu()
    {
        if (PaperPageTransition.IsTransitioning) return;
        if (!PhotonNetwork.InRoom) { PaperPageTransition.LoadScene("SampleScene"); return; }
        PaperPageTransition.CoverNetworkLoad(() => { if (!PhotonNetwork.LeaveRoom()) UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("SampleScene"); });
    }

    public override void OnLeftRoom()
    {
        if (PaperPageTransition.IsTransitioning)
            UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("SampleScene");
        else PaperPageTransition.LoadScene("SampleScene");
    }
    public override void OnDisconnected(DisconnectCause cause)
    {
        PaperPageTransition.Instance?.CancelNetworkWait();
        PaperPageTransition.LoadScene("SampleScene");
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        if (!isGameOver && (FriendMatchService.Instance?.IsPlaying ?? false))
        {
            isGameOver = true; winLossPanel.SetActive(true); rematchButton.interactable = false;
            resultText.text = "Arkadaşın odadan ayrıldı.\nMenüye dönülüyor…";
            DOVirtual.DelayedCall(2f, () => LeaveToMenu()).SetLink(gameObject);
            return;
        }
        // Rakip oyundan çıkarsa rematch butonunu kapat ve bilgi ver
        if (isGameOver)
        {
            rematchButton.interactable = false;
            rematchButtonText.text = "Rakip Ayrıldı";
            resultText.text = "Rakip oyundan ayrıldı.\nMenüye dönülüyor...";
            DOVirtual.DelayedCall(2f, () => LeaveToMenu()).SetLink(gameObject);
        }
    }

    // ... (GetWordList ve diğer RPC'lerin aynı kalıyor) ...
    [PunRPC] void RPC_OpponentGuessedCorrect(int[] indices) { /* Mevcut kodun */ foreach (int i in indices) opponentBoxes[i].GetComponent<Image>().DOColor(correctLetterColor, 0.2f); }
    [PunRPC] void RPC_PlayerHanged() { isOpponentDead = true; CheckDrawCondition(); }
    void CheckDrawCondition()
    {
        if (amIDead && isOpponentDead)
            photonView.RPC("RPC_GameOver", RpcTarget.All, -1); // -1 berabere demek
    }
    List<string> GetWordList(int idx) { if (wordDatabase == null) return new List<string> { "HATA" }; switch (idx) { case 0: return wordDatabase.lolWords; case 1: return wordDatabase.valorantWords; case 2: return wordDatabase.csWords; case 3: return wordDatabase.generalWords; case 4: return wordDatabase.countryWords; case 5: return wordDatabase.minecraftWords; default: return wordDatabase.lolWords; } }
}
