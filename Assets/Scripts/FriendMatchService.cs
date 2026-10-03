using System;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class FriendMatchService : MonoBehaviourPunCallbacks
{
    public static FriendMatchService Instance { get; private set; }
    public bool IsBusy => state != "Idle";
    public bool IsPlaying => state == "Playing";
    public string Message { get; private set; } = "";
    public SocialInvite ActiveInvite { get; private set; }
    public event Action Changed;
    string state = "Idle", target, room, inviteId;
    int category, generation;
    bool starting;
    float deadline;
    void Awake() { Instance = this; }
    public override void OnEnable() { base.OnEnable(); FriendsService.Instance.Changed += SocialChanged; }
    public override void OnDisable() { if (FriendsService.Instance != null) FriendsService.Instance.Changed -= SocialChanged; base.OnDisable(); }
    void Status(string value) { Message = value; Changed?.Invoke(); }
    void Update()
    {
        if (state == "Idle" || IsPlaying) return;
        if (Time.realtimeSinceStartup > deadline) Cancel("Davetin veya oda bağlantısının süresi doldu.");
    }
    public void Invite(SocialPlayer friend, int selectedCategory)
    {
        if (IsBusy || FriendsService.Instance.IsBusy || !friend.available || selectedCategory < 0 || selectedCategory > 5) return;
        state = "ConnectingHost"; target = friend.id; category = selectedCategory; inviteId = Guid.NewGuid().ToString("N"); room = "friend-" + Guid.NewGuid().ToString("N");
        deadline = Time.realtimeSinceStartup + 25; int epoch = ++generation;
        FriendsService.Instance.SetPresence("hosting"); Status("Arkadaş odası hazırlanıyor…");
        AccountService.Instance.ConnectPhoton(ok => {
            if (epoch != generation) return;
            if (!ok) { Cancel(AccountService.Instance.Message); return; }
            state = "CreatingRoom";
            var options = new RoomOptions { MaxPlayers = 2, IsVisible = false, IsOpen = true, PublishUserId = true,
                CustomRoomProperties = new Hashtable { { "C", category }, { "F", inviteId }, { "Host", AccountService.Instance.PlayerId }, { "Guest", target } } };
            if (!PhotonNetwork.CreateRoom(room, options, TypedLobby.Default, new[] { target })) Cancel("Arkadaş odası oluşturulamadı.");
        });
    }
    public void Accept(SocialInvite invite)
    {
        if (IsBusy || FriendsService.Instance.IsBusy || invite == null || invite.to != AccountService.Instance.PlayerId) return;
        state = "Accepting"; ActiveInvite = invite; inviteId = invite.id; target = invite.from; room = invite.room; category = invite.category;
        deadline = Time.realtimeSinceStartup + 25; int epoch = ++generation;
        Status("Davet kabul ediliyor…");
        FriendsService.Instance.Execute("answerInvite", new Dictionary<string, object> { { "host", invite.from }, { "inviteId", invite.id }, { "decision", "accept" } }, reply => {
            if (epoch != generation) return;
            if (!reply.ok) { Cancel(FriendsService.Translate(reply.error)); return; }
            ActiveInvite = reply.invite; FriendsService.Instance.SetPresence("joining");
            AccountService.Instance.ConnectPhoton(ok => {
                if (epoch != generation) return;
                if (!ok) { Cancel(AccountService.Instance.Message); return; }
                state = "JoiningRoom"; Status("Arkadaşının odasına giriliyor…");
                if (!PhotonNetwork.JoinRoom(room)) Cancel("Arkadaş odasına giriş başlatılamadı.");
            });
        });
    }
    public void Decline(SocialInvite invite)
    {
        FriendsService.Instance.Execute("answerInvite", new Dictionary<string, object> { { "host", invite.from }, { "inviteId", invite.id }, { "decision", "decline" } }, null);
    }
    public override void OnJoinedRoom()
    {
        if (state == "CreatingRoom")
        {
            state = "Publishing"; int epoch = generation;
            FriendsService.Instance.Execute("invite", new Dictionary<string, object> { { "target", target }, { "inviteId", inviteId }, { "room", room }, { "category", category }, { "region", "eu" } }, reply => {
                if (epoch != generation) return;
                if (!reply.ok) { Cancel(FriendsService.Translate(reply.error)); return; }
                ActiveInvite = reply.invite; state = "Waiting"; deadline = Time.realtimeSinceStartup + 60; Status("Arkadaşının daveti kabul etmesi bekleniyor…");
                TryStart();
            });
        }
        else if (state == "JoiningRoom")
        {
            var props = PhotonNetwork.CurrentRoom.CustomProperties;
            if (!Equals(props["F"], inviteId) || !Equals(props["Host"], target) || !Equals(props["Guest"], AccountService.Instance.PlayerId)) { Cancel("Davet ve oda bilgileri uyuşmuyor."); return; }
            state = "WaitingGuest"; Status("Maç hazırlanıyor…"); FriendsService.Instance.SetPresence("match");
        }
    }
    public override void OnPlayerEnteredRoom(Player newPlayer) { if (IsBusy) TryStart(); }
    void TryStart()
    {
        if (starting || state != "Waiting" || !PhotonNetwork.IsMasterClient || PhotonNetwork.CurrentRoom.PlayerCount != 2) return;
        var expected = new[] { AccountService.Instance.PlayerId, target };
        var actual = PhotonNetwork.PlayerList.Select(p => p.UserId).ToArray();
        if (!actual.OrderBy(x => x).SequenceEqual(expected.OrderBy(x => x)))
        {
            foreach (var player in PhotonNetwork.PlayerListOthers.Where(p => p.UserId != target)) PhotonNetwork.CloseConnection(player);
            Status("Davet edilen arkadaş bekleniyor…"); return;
        }
        starting = true; int epoch = generation;
        FriendsService.Instance.Call("startMatch", new Dictionary<string, object> { { "host", AccountService.Instance.PlayerId }, { "inviteId", inviteId } }, reply => {
            if (epoch != generation) return; starting = false;
            if (!reply.ok) { Cancel(FriendsService.Translate(reply.error)); return; }
            state = "Playing"; ActiveInvite = reply.invite; PhotonNetwork.CurrentRoom.IsOpen = false;
            FriendsService.Instance.SetPresence("match"); Status("Maç başlıyor…");
            PaperPageTransition.CoverNetworkLoad(() => PhotonNetwork.LoadLevel("MultiplayerGameScene"));
        });
    }
    void SocialChanged()
    {
        if (!IsBusy || IsPlaying || ActiveInvite == null) return;
        var current = FriendsService.Instance.Snapshot.invite;
        if (current != null && current.id == inviteId && (current.status == "cancelled" || current.status == "declined" || current.status == "expired")) Cancel("Davet sona erdi.");
    }
    public void Cancel() => Cancel("Davet iptal edildi.");
    public void Cancel(string message)
    {
        if (!IsBusy) return;
        if (!string.IsNullOrEmpty(inviteId) && AccountService.Instance.IsLoggedIn)
            FriendsService.Instance.Call("finishInvite", new Dictionary<string, object> { { "host", ActiveInvite != null ? ActiveInvite.from : AccountService.Instance.PlayerId }, { "inviteId", inviteId } }, null);
        generation++; state = "Idle"; starting = false; ActiveInvite = null;
        if (PhotonNetwork.InRoom) PhotonNetwork.LeaveRoom();
        FriendsService.Instance.SetPresence(SceneManager.GetActiveScene().name == "SampleScene" ? "menu" : "solo"); Status(message);
    }
    public override void OnCreateRoomFailed(short returnCode, string message) { if (IsBusy) Cancel("Arkadaş odası oluşturulamadı."); }
    public override void OnJoinRoomFailed(short returnCode, string message) { if (IsBusy) Cancel("Arkadaş odası kapandı veya doldu."); }
    public override void OnDisconnected(DisconnectCause cause) { if (IsBusy) Cancel("Oyun sunucusuyla bağlantı kesildi."); }
    public override void OnLeftRoom() { if (IsBusy) Cancel("Odadan ayrıldın."); }
    public override void OnPlayerLeftRoom(Player otherPlayer) { if (IsBusy && !IsPlaying) Cancel("Arkadaşın odadan ayrıldı."); }
    public void SceneReady(string scene)
    {
        if (scene == "MultiplayerGameScene" && IsBusy) { state = "Playing"; FriendsService.Instance.SetPresence("match"); }
    }
}
