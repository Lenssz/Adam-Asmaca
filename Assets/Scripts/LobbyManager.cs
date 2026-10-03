using UnityEngine;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class LobbyManager : MonoBehaviourPunCallbacks
{
    [Header("UI Panelleri")]
    public GameObject loginPanel, modeSelectionPanel, categoryPanel, waitingPanel, profilePanel;
    [Header("Profil Elemanları")]
    public TMP_InputField nameInput;
    public TMP_Text currentNameText;
    bool isMultiplayerMode;
    int selectedMultiplayerCategory = -1;

    void Start()
    {
        string savedName = AccountService.Instance != null && AccountService.Instance.IsLoggedIn ? AccountService.Instance.DisplayName : PlayerPrefs.GetString("PlayerName", "Oyuncu_" + Random.Range(100, 999));
        PhotonNetwork.NickName = savedName;
        if (currentNameText != null) currentNameText.text = savedName;
        if (nameInput != null) nameInput.text = savedName;
        Activate(modeSelectionPanel);
    }
    void Activate(GameObject panel)
    {
        if (this == null) return;
        GetComponent<PaperAccountUI>()?.HidePages();
        foreach (var candidate in new[] { loginPanel, modeSelectionPanel, categoryPanel, waitingPanel, profilePanel })
            if (candidate != null) candidate.SetActive(candidate == panel);
    }
    void Show(GameObject panel, bool authoritative = false)
    {
        float seconds = PaperPageTransition.Instance != null ? PaperPageTransition.Instance.categoryEntryDuration : .7f;
        PaperPageTransition.ShowPanel(() => { if (this != null) Activate(panel); }, seconds, authoritative);
    }
    public void OpenProfile()
    {
        if (PaperPageTransition.IsTransitioning) return;
        if (nameInput != null) nameInput.text = PhotonNetwork.NickName;
        Show(profilePanel);
    }
    public void SaveAndCloseProfile()
    {
        if (PaperPageTransition.IsTransitioning) return;
        string newName = nameInput.text;
        if (AccountService.Instance != null && AccountService.Instance.IsLoggedIn)
        {
            AccountService.Instance.SaveDisplayName(newName, ok => { if (this != null && ok) { currentNameText.text = AccountService.Instance.DisplayName; Show(modeSelectionPanel); } });
            return;
        }
        if (!string.IsNullOrEmpty(newName))
        {
            if (newName.Length > 10) newName = newName.Substring(0, 10);
            PhotonNetwork.NickName = newName; PlayerPrefs.SetString("PlayerName", newName);
            if (currentNameText != null) currentNameText.text = newName;
        }
        Show(modeSelectionPanel);
    }
    public void SelectSingleplayerMode()
    {
        if (PaperPageTransition.IsTransitioning) return;
        isMultiplayerMode = false; Show(categoryPanel);
    }
    public void SelectMultiplayerMode()
    {
        if (PaperPageTransition.IsTransitioning) return;
        if (AccountService.Instance == null || !AccountService.Instance.IsLoggedIn) { GetComponent<PaperAccountUI>()?.OpenAuth(true); return; }
        isMultiplayerMode = true; Show(loginPanel);
        // Connection starts now; the animation only delays the visible panel change.
        AccountService.Instance.ConnectPhoton(ok => { if (this == null) return; if (ok) OnJoinedLobby(); else { isMultiplayerMode = false; Show(modeSelectionPanel, true); } });
    }
    public void BackToModeSelection()
    {
        if (PaperPageTransition.IsTransitioning) return;
        isMultiplayerMode = false; Show(modeSelectionPanel);
        if (PhotonNetwork.IsConnected) PhotonNetwork.Disconnect();
    }
    public override void OnConnectedToMaster()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
    }
    public override void OnJoinedLobby() { if (isMultiplayerMode && !(FriendMatchService.Instance?.IsBusy ?? false)) Show(categoryPanel, true); }
    public override void OnDisconnected(DisconnectCause cause)
    {
        PaperPageTransition.Instance?.CancelNetworkWait();
        if (isMultiplayerMode) { isMultiplayerMode = false; Show(modeSelectionPanel, true); }
    }
    public void OnCategoryButtonClicked(int categoryIndex)
    {
        if (PaperPageTransition.IsTransitioning) return;
        if (!isMultiplayerMode) { GameManager.Instance.SelectCategory(categoryIndex); return; }
        selectedMultiplayerCategory = categoryIndex; Show(waitingPanel);
        FriendsService.Instance?.SetPresence("queue");
        PhotonNetwork.JoinRandomRoom(new Hashtable { { "C", selectedMultiplayerCategory } }, 2);
    }
    public void LeaveQueue()
    {
        if (PaperPageTransition.IsTransitioning) return;
        Show(categoryPanel);
        FriendsService.Instance?.SetPresence("menu");
        if (PhotonNetwork.InRoom) PhotonNetwork.LeaveRoom();
    }
    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        if (!isMultiplayerMode || (FriendMatchService.Instance?.IsBusy ?? false)) return;
        var options = new RoomOptions { MaxPlayers = 2, PublishUserId = true, CustomRoomProperties = new Hashtable { { "C", selectedMultiplayerCategory } }, CustomRoomPropertiesForLobby = new[] { "C" } };
        PhotonNetwork.CreateRoom(null, options);
    }
    public override void OnJoinedRoom() { if (isMultiplayerMode && !(FriendMatchService.Instance?.IsBusy ?? false) && PhotonNetwork.CurrentRoom.PlayerCount == 2) StartMultiplayerMatch(); }
    public override void OnPlayerEnteredRoom(Player newPlayer) { if (isMultiplayerMode && !(FriendMatchService.Instance?.IsBusy ?? false) && PhotonNetwork.CurrentRoom.PlayerCount == 2) StartMultiplayerMatch(); }
    void StartMultiplayerMatch()
    {
        if (PhotonNetwork.IsMasterClient) PaperPageTransition.CoverNetworkLoad(() => PhotonNetwork.LoadLevel("MultiplayerGameScene"));
    }
}
