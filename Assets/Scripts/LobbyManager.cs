using UnityEngine;
using UnityEngine.UI;
using TMPro; // TextMeshPro eklendi
using Photon.Pun;
using Photon.Realtime;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class LobbyManager : MonoBehaviourPunCallbacks
{
    [Header("UI Panelleri")]
    public GameObject loginPanel;
    public GameObject modeSelectionPanel;
    public GameObject categoryPanel;
    public GameObject waitingPanel;
    public GameObject profilePanel;         // YENİ: Profil Paneli

    [Header("Profil Elemanları")]
    public TMP_InputField nameInput;        // YENİ: İsim Yazma Alanı
    public TMP_Text currentNameText;        // YENİ: Ana Menüdeki İsim Yazısı

    private bool isMultiplayerMode = false;
    private int selectedMultiplayerCategory = -1;

    void Start()
    {
        // 1. HAFIZADAN İSMİ YÜKLE
        LoadPlayerNickname();

        loginPanel.SetActive(false);
        categoryPanel.SetActive(false);
        waitingPanel.SetActive(false);
        profilePanel.SetActive(false); // Başlangıçta kapalı
        modeSelectionPanel.SetActive(true);
    }

    // --- PROFİL SİSTEMİ ---

    private void LoadPlayerNickname()
    {
        // Daha önce kaydedilmiş isim var mı bak, yoksa rastgele ata
        string savedName = PlayerPrefs.GetString("PlayerName", "Oyuncu_" + Random.Range(100, 999));
        PhotonNetwork.NickName = savedName;

        if (currentNameText != null) currentNameText.text = savedName;
        if (nameInput != null) nameInput.text = savedName;
    }

    public void OpenProfile()
    {
        // 1. Mevcut ismi kutuya yazdır
        if (nameInput != null)
        {
            nameInput.text = PhotonNetwork.NickName;
        }

        // 2. Ana menü panelini kapat, profil panelini aç
        modeSelectionPanel.SetActive(false);
        profilePanel.SetActive(true);
    }

    public void SaveAndCloseProfile()
    {
        string newName = nameInput.text;

        // 1. Boşluk kontrolü (İsim silinmesini engeller)
        if (!string.IsNullOrEmpty(newName))
        {
            // 2. Karakter sınırı kontrolü
            if (newName.Length > 10)
            {
                newName = newName.Substring(0, 10);
            }

            PhotonNetwork.NickName = newName;
            PlayerPrefs.SetString("PlayerName", newName);

            if (currentNameText != null) currentNameText.text = newName;
        }

        // HER DURUMDA: Panelleri eski haline getir
        profilePanel.SetActive(false);
        modeSelectionPanel.SetActive(true); // Bunu eklemeyi unutma!
    }

    // --- BUTON FONKSİYONLARI ---

    public void SelectSingleplayerMode()
    {
        isMultiplayerMode = false;
        modeSelectionPanel.SetActive(false);
        categoryPanel.SetActive(true);
    }

    public void SelectMultiplayerMode()
    {
        isMultiplayerMode = true;
        modeSelectionPanel.SetActive(false);
        loginPanel.SetActive(true); // "Bağlanılıyor..." yazısı

        if (!PhotonNetwork.IsConnected)
        {
            Debug.Log("Photon Sunucularına Bağlanılıyor...");
            PhotonNetwork.ConnectUsingSettings();
        }
        else if (!PhotonNetwork.InLobby)
        {
            // Bağlı ama lobide değilse lobiye sok
            PhotonNetwork.JoinLobby();
        }
        else
        {
            // Zaten lobideyse direkt kategorileri göster
            OnJoinedLobby();
        }
    }

    public void BackToModeSelection()
    {
        categoryPanel.SetActive(false);
        waitingPanel.SetActive(false); // Bekleme ekranından döndüyse onu da kapat
        modeSelectionPanel.SetActive(true);

        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.Disconnect(); // Gerekirse bağlantıyı kes
        }
    }

    // --- PHOTON BAĞLANTI AŞAMALARI ---

    public override void OnConnectedToMaster()
    {
        Debug.Log("Sunucuya bağlandık, lobiye giriyoruz...");
        PhotonNetwork.JoinLobby();
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("Lobiye giriş yapıldı. Arayüz sıfırlanıyor...");

        // 1. ÖNCE HER ŞEYİ KAPAT (Temizlik)
        loginPanel.SetActive(false);
        categoryPanel.SetActive(false);
        waitingPanel.SetActive(false);
        profilePanel.SetActive(false);

        // 2. EĞER ÇOK OYUNCULU MODDAN DÖNÜLÜYORSA
        if (isMultiplayerMode)
        {
            // Oyuncu lobiye geri döndü, kategorileri seçmesi için kategori panelini aç
            categoryPanel.SetActive(true);
            modeSelectionPanel.SetActive(false);
        }
        else
        {
            // Eğer oyun ilk kez açılıyorsa veya tam bir reset lazımsa mod seçimine dön
            modeSelectionPanel.SetActive(true);
        }
    }

    // --- KATEGORİ SEÇİMİ VE EŞLEŞTİRME ---

    public void OnCategoryButtonClicked(int categoryIndex)
    {
        categoryPanel.SetActive(false);

        if (isMultiplayerMode == false)
        {
            GameManager.Instance.SelectCategory(categoryIndex);
        }
        else
        {
            waitingPanel.SetActive(true);
            selectedMultiplayerCategory = categoryIndex;

            Hashtable expectedCustomRoomProperties = new Hashtable() { { "C", selectedMultiplayerCategory } };
            PhotonNetwork.JoinRandomRoom(expectedCustomRoomProperties, 2);
        }
    }

    // --- ODA FONKSİYONLARI ---

    public void LeaveQueue()
    {
        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
        }
        waitingPanel.SetActive(false);
        categoryPanel.SetActive(true);
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        RoomOptions roomOptions = new RoomOptions() { MaxPlayers = 2 };
        roomOptions.CustomRoomProperties = new Hashtable() { { "C", selectedMultiplayerCategory } };
        roomOptions.CustomRoomPropertiesForLobby = new string[] { "C" };

        PhotonNetwork.CreateRoom(null, roomOptions);
    }

    public override void OnJoinedRoom()
    {
        if (PhotonNetwork.CurrentRoom.PlayerCount == 2) StartMultiplayerMatch();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        if (PhotonNetwork.CurrentRoom.PlayerCount == 2) StartMultiplayerMatch();
    }

    void StartMultiplayerMatch()
    {
        if (PhotonNetwork.IsMasterClient)
            PhotonNetwork.LoadLevel("MultiplayerGameScene");
    }
}