using System;
using System.Collections.Generic;
using PlayFab;
using PlayFab.ClientModels;
using PlayFab.Json;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FriendsService : MonoBehaviour
{
    public static FriendsService Instance { get; private set; }
    public SocialSnapshot Snapshot { get; private set; } = new SocialSnapshot();
    public bool IsBusy { get; private set; }
    public string Message { get; private set; } = "";
    public string Presence { get; private set; } = "menu";
    public event Action Changed;
    float nextPoll, nextHeartbeat;
    bool focused = true, polling, heartbeat;
    string owner;
    int generation;
#if UNITY_EDITOR
    public Func<string, Dictionary<string, object>, SocialReply> ValidationTransport;
#endif
    void Awake() { Instance = this; }
    void OnEnable() { AccountService.Instance.Changed += AccountChanged; SceneManager.sceneLoaded += SceneLoaded; }
    void OnDisable() { if (AccountService.Instance != null) AccountService.Instance.Changed -= AccountChanged; SceneManager.sceneLoaded -= SceneLoaded; }
    void AccountChanged()
    {
        var id = AccountService.Instance.PlayerId;
        if (owner == id) return;
        owner = id; generation++; Snapshot = new SocialSnapshot(); IsBusy = polling = heartbeat = false; Message = ""; nextPoll = nextHeartbeat = 0; Changed?.Invoke();
    }
    void SceneLoaded(Scene scene, LoadSceneMode mode) { SetPresence(scene.name == "SampleScene" ? "menu" : scene.name == "MultiplayerGameScene" ? "match" : "solo"); }
    void Update()
    {
        if (!focused || !AccountService.Instance.IsLoggedIn) return;
        if (Time.realtimeSinceStartup >= nextHeartbeat && !heartbeat) Heartbeat();
        if (Time.realtimeSinceStartup >= nextPoll && !polling && !IsBusy) Refresh();
    }
    void OnApplicationFocus(bool value) { focused = value; if (!value) Offline(); else { nextPoll = nextHeartbeat = 0; } }
    void OnApplicationPause(bool value) => OnApplicationFocus(!value);
    public void SetPresence(string value) { Presence = value; nextHeartbeat = 0; }
    public void Offline() { if (AccountService.Instance.IsLoggedIn) Call("presence", new Dictionary<string, object> { { "state", "offline" } }, null); }
    void Heartbeat()
    {
        heartbeat = true; nextHeartbeat = Time.realtimeSinceStartup + 20; int epoch = generation;
        Call("presence", new Dictionary<string, object> { { "state", Presence } }, result => { if (epoch == generation) heartbeat = false; });
    }
    public void Refresh()
    {
        if (polling || !AccountService.Instance.IsLoggedIn) return;
        polling = true; nextPoll = Time.realtimeSinceStartup + 5; int epoch = generation;
        ReadPage(new SocialSnapshot(), 0, epoch);
    }
    void ReadPage(SocialSnapshot accumulated, int cursor, int epoch)
    {
        Call("snapshot", new Dictionary<string, object> { { "cursor", cursor } }, result => {
            if (epoch != generation) return; polling = false;
            if (result.ok && result.snapshot != null) {
                accumulated.friends.AddRange(result.snapshot.friends ?? new List<SocialPlayer>());
                accumulated.incoming.AddRange(result.snapshot.incoming ?? new List<SocialRequest>());
                accumulated.outgoing.AddRange(result.snapshot.outgoing ?? new List<SocialRequest>());
                accumulated.invite = result.snapshot.invite;
                if (result.snapshot.hasMore && result.snapshot.nextCursor > cursor && result.snapshot.nextCursor <= 2000) { polling = true; ReadPage(accumulated, result.snapshot.nextCursor, epoch); return; }
                Snapshot = accumulated; Message = "";
            }
            else Message = Translate(result.error); Changed?.Invoke();
        });
    }
    public void Search(string value, Action<SocialPlayer> done)
    {
        if (IsBusy || !AccountService.Instance.IsLoggedIn) return;
        value = (value ?? "").Trim();
        if (value.Length < 3) { Message = "Oyuncu ID veya kullanıcı adı yaz."; Changed?.Invoke(); done?.Invoke(null); return; }
        IsBusy = true; int epoch = generation; Message = "Oyuncu aranıyor…"; Changed?.Invoke();
        Action<PlayFabError> fail = error => { if (epoch != generation) return; IsBusy = false; Message = "Oyuncu bulunamadı."; Changed?.Invoke(); done?.Invoke(null); };
        Action<GetAccountInfoResult> found = result => {
            if (epoch != generation) return; IsBusy = false;
            Execute("search", new Dictionary<string, object> { { "target", result.AccountInfo.PlayFabId } }, reply => done?.Invoke(reply.ok ? reply.player : null), false);
        };
        if (System.Text.RegularExpressions.Regex.IsMatch(value, "^[A-Fa-f0-9]{8,32}$"))
            PlayFabClientAPI.GetAccountInfo(new GetAccountInfoRequest { PlayFabId = value }, found, error => { if (epoch == generation) PlayFabClientAPI.GetAccountInfo(new GetAccountInfoRequest { Username = value }, found, fail); });
        else PlayFabClientAPI.GetAccountInfo(new GetAccountInfoRequest { Username = value }, found, fail);
    }
    public void SendRequest(string id) => Execute("request", new Dictionary<string, object> { { "target", id }, { "requestId", Guid.NewGuid().ToString("N") } }, null);
    public void Respond(SocialRequest request, string action) => Execute("respond", new Dictionary<string, object> { { "target", request.from == owner ? request.to : request.from }, { "requestId", request.id }, { "decision", action } }, null);
    public void Remove(string id) => Execute("remove", new Dictionary<string, object> { { "target", id } }, null);
    public void Execute(string action, Dictionary<string, object> data, Action<SocialReply> done, bool refresh = true)
    {
        if (IsBusy || !AccountService.Instance.IsLoggedIn) return;
        IsBusy = true; Message = "İşlem yapılıyor…"; Changed?.Invoke(); int epoch = generation;
        Call(action, data, result => {
            if (epoch != generation) return; IsBusy = false; Message = result.ok ? "İşlem tamamlandı." : Translate(result.error); Changed?.Invoke(); done?.Invoke(result);
            if (refresh && result.ok) { nextPoll = 0; Refresh(); }
        });
    }
    public void Call(string action, Dictionary<string, object> data, Action<SocialReply> done)
    {
        if (!AccountService.Instance.IsLoggedIn) { done?.Invoke(new SocialReply { error = "login_required" }); return; }
#if UNITY_EDITOR
        if (ValidationTransport != null) { done?.Invoke(ValidationTransport(action, data)); return; }
#endif
        var args = data != null ? new Dictionary<string, object>(data) : new Dictionary<string, object>(); args["action"] = action;
        int epoch = generation;
        PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest { FunctionName = "Social", FunctionParameter = args, GeneratePlayStreamEvent = false }, result => {
            if (epoch != generation) return;
            SocialReply reply;
            try {
                var serializer = PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer);
                reply = result.Error != null ? new SocialReply { error = "backend_unavailable" } : serializer.DeserializeObject<SocialReply>(serializer.SerializeObject(result.FunctionResult));
            } catch { reply = new SocialReply { error = "backend_unavailable" }; }
            done?.Invoke(reply ?? new SocialReply { error = "backend_unavailable" });
        }, error => { if (epoch == generation) done?.Invoke(new SocialReply { error = error.Error == PlayFabErrorCode.NotAuthenticated ? "login_required" : "network_error" }); });
    }
    public static string Translate(string error)
    {
        switch (error)
        {
            case "not_found": return "Oyuncu bulunamadı.";
            case "self": return "Kendine istek gönderemezsin.";
            case "already_friends": return "Zaten arkadaşsınız.";
            case "pending": return "Aranızda bekleyen bir istek var.";
            case "stale": return "Bu istek artık geçerli değil. Listeyi yenile.";
            case "not_allowed": return "Bu işlem için yetkin yok.";
            case "busy": return "Oyuncu şu anda müsait değil.";
            case "expired": return "Davetin süresi doldu.";
            case "not_friends": return "Önce arkadaşlık isteği kabul edilmeli.";
            case "limit": return "Arkadaş veya bekleyen istek sınırına ulaşıldı.";
            case "login_required": return "Yeniden giriş yapman gerekiyor.";
            case "backend_unavailable": return "Arkadaşlık sunucusu henüz hazır değil.";
            default: return "İşlem tamamlanamadı. Bağlantını kontrol edip tekrar dene.";
        }
    }
}
