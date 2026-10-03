using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using PlayFab;
using PlayFab.ClientModels;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AccountService : MonoBehaviourPunCallbacks
{
    public static AccountService Instance { get; private set; }
    public bool IsLoggedIn => !string.IsNullOrEmpty(PlayerId);
    public bool IsBusy { get; private set; }
    public string PlayerId { get; private set; }
    public string Username { get; private set; }
    public string DisplayName { get; private set; }
    public string Message { get; private set; } = "";
    public string RememberedUsername => PlayerPrefs.GetString("AccountUsername", "");
    public bool RememberLogin { get; private set; }
    public bool IsRestoring { get; private set; }
    string rememberedCustomId;
    public event Action Changed;
    int generation;
    bool connecting;
    bool reconnecting;
    Action photonStart;
    float connectionDeadline;
    readonly List<Action<bool>> connectionCallbacks = new List<Action<bool>>();
    public const string OnlineVersion = "paper-social-v1";
#if UNITY_EDITOR
    public void ValidationLogin(string id, string username, string name)
    {
        PlayerId = id; Username = username; DisplayName = name; PhotonNetwork.NickName = name; Changed?.Invoke();
    }
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ClearStatics() { Instance = null; }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        var root = new GameObject("PlayFab account and friends");
        DontDestroyOnLoad(root);
        root.AddComponent<AccountService>(); root.AddComponent<FriendsService>(); root.AddComponent<FriendMatchService>();
    }
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }
    void Start() => RestoreRememberedAccount();
    public void RestoreRememberedAccount()
    {
        if(IsBusy || IsLoggedIn || !RememberedAccountStore.TryRead(out var saved))return;
        RememberLogin=true;rememberedCustomId=saved.customId;IsBusy=IsRestoring=true;
        int epoch=generation;SetMessage("Hesabına otomatik giriş yapılıyor…");
        PlayFabClientAPI.LoginWithCustomID(new LoginWithCustomIDRequest{CustomId=saved.customId,CreateAccount=false,InfoRequestParameters=new GetPlayerCombinedInfoRequestParams{GetUserAccountInfo=true}},result=>{
            if(epoch!=generation)return;
            if(result.PlayFabId!=saved.playerId){RememberedAccountStore.Clear();RememberLogin=false;rememberedCustomId=null;IsBusy=IsRestoring=false;PlayFabClientAPI.ForgetAllCredentials();SetMessage("Kayıtlı giriş doğrulanamadı. Şifrenle giriş yap.");return;}
            IsRestoring=false;
            var info=result.InfoResultPayload?.AccountInfo;
            LoggedIn(epoch,result.PlayFabId,info?.Username??saved.username,info?.TitleInfo?.DisplayName??saved.username,null);
        },error=>{
            if(epoch!=generation)return;IsBusy=IsRestoring=false;
            if(error.Error==PlayFabErrorCode.AccountNotFound || error.Error==PlayFabErrorCode.AccountBanned){RememberedAccountStore.Clear();RememberLogin=false;rememberedCustomId=null;SetMessage("Kayıtlı giriş sona erdi. Şifrenle giriş yap.");}
            else SetMessage("Otomatik giriş yapılamadı. Bağlantı gelince tekrar deneyebilirsin.");
        });
    }
    public override void OnEnable() { base.OnEnable(); SceneManager.sceneLoaded += SceneLoaded; }
    public override void OnDisable() { SceneManager.sceneLoaded -= SceneLoaded; base.OnDisable(); }
    void SceneLoaded(Scene scene, LoadSceneMode mode)
    {
        FriendMatchService.Instance?.SceneReady(scene.name);
        if (scene.name != "SampleScene") return;
        var lobby = FindFirstObjectByType<LobbyManager>();
        if (lobby != null && lobby.GetComponent<PaperAccountUI>() == null) lobby.gameObject.AddComponent<PaperAccountUI>();
    }
    void Update() { if (connecting && Time.realtimeSinceStartup > connectionDeadline) FinishConnection(false, "Sunucuya bağlantı zaman aşımına uğradı."); }
    public static string ValidateCredentials(string username, string password)
    {
        if (username == null || !Regex.IsMatch(username, @"^[A-Za-z0-9]{3,20}$")) return "Kullanıcı adı 3–20 İngilizce harf veya rakamdan oluşmalı.";
        if (password == null || password.Length < 6 || password.Length > 100) return "Şifre 6–100 karakter olmalı.";
        return null;
    }
    public void Authenticate(string username, string password, bool register, Action<bool> done, bool remember = false)
    {
        if (IsBusy || IsLoggedIn) return;
        username = (username ?? "").Trim();
        var problem = ValidateCredentials(username, password);
        if (problem == null && string.IsNullOrWhiteSpace(PlayFabSettings.staticSettings.TitleId)) problem = "PlayFab oyun kimliği henüz ayarlanmamış.";
        if (problem != null) { SetMessage(problem); done?.Invoke(false); return; }
        IsBusy = true; SetMessage(register ? "Hesap oluşturuluyor…" : "Giriş yapılıyor…"); int epoch = generation;
        Action<PlayFabError> fail = error => { if (epoch != generation) return; IsBusy = false; SetMessage(ErrorMessage(error)); done?.Invoke(false); };
        if (register)
            PlayFabClientAPI.RegisterPlayFabUser(new RegisterPlayFabUserRequest { Username = username, Password = password, RequireBothUsernameAndEmail = false, DisplayName = username.Substring(0, Math.Min(10, username.Length)) },
                result => FinishRememberedLogin(epoch, result.PlayFabId, username, username.Substring(0, Math.Min(10, username.Length)), remember, done), fail);
        else
            PlayFabClientAPI.LoginWithPlayFab(new LoginWithPlayFabRequest { Username = username, Password = password, InfoRequestParameters = new GetPlayerCombinedInfoRequestParams { GetUserAccountInfo = true } },
                result => FinishRememberedLogin(epoch, result.PlayFabId, username, result.InfoResultPayload?.AccountInfo?.TitleInfo?.DisplayName ?? username.Substring(0, Math.Min(10, username.Length)), remember, done), fail);
    }
    void FinishRememberedLogin(int epoch,string id,string username,string name,bool remember,Action<bool> done)
    {
        if(epoch!=generation)return;
        if(!remember || !RememberedAccountStore.Supported) {
            if(RememberedAccountStore.TryRead(out var previous) && previous.playerId==id)
                PlayFabClientAPI.UnlinkCustomID(new UnlinkCustomIDRequest{CustomId=previous.customId},_=>{},_=>{});
            RememberedAccountStore.Clear();RememberLogin=false;rememberedCustomId=null;
            LoggedIn(epoch,id,username,name,done);return;
        }
        var bytes=new byte[32];using(var random=System.Security.Cryptography.RandomNumberGenerator.Create())random.GetBytes(bytes);
        var customId="remember-"+BitConverter.ToString(bytes).Replace("-","").ToLowerInvariant();Array.Clear(bytes,0,bytes.Length);
        SetMessage("Bu cihazda hesap hatırlanıyor…");
        PlayFabClientAPI.LinkCustomID(new LinkCustomIDRequest{CustomId=customId,ForceLink=false},_=>{
            if(epoch!=generation)return;
            RememberLogin=RememberedAccountStore.Save(new RememberedAccountStore.Record{customId=customId,playerId=id,username=username});
            rememberedCustomId=RememberLogin?customId:null;
            if(!RememberLogin)PlayFabClientAPI.UnlinkCustomID(new UnlinkCustomIDRequest{CustomId=customId},result=>{},error=>{});
            LoggedIn(epoch,id,username,name,done);
            if(!RememberLogin)SetMessage("Giriş yapıldı; bu cihazda güvenli hesap kaydı yapılamadı.");
        },error=>{
            if(epoch!=generation)return;RememberedAccountStore.Clear();RememberLogin=false;rememberedCustomId=null;
            LoggedIn(epoch,id,username,name,done);SetMessage("Giriş yapıldı; hesabı hatırlama ayarlanamadı. Tekrar giriş yaparken deneyebilirsin.");
        });
    }
    void LoggedIn(int epoch, string id, string username, string name, Action<bool> done)
    {
        if (epoch != generation) { PlayFabClientAPI.ForgetAllCredentials(); return; }
        PlayerId = id; Username = username; DisplayName = name; IsBusy = false;
        PlayerPrefs.SetString("AccountUsername", username); PlayerPrefs.Save();
        PhotonNetwork.NickName = DisplayName; SetMessage("Giriş yapıldı."); done?.Invoke(true);
    }
    public void SaveDisplayName(string name, Action<bool> done)
    {
        if (!IsLoggedIn || IsBusy) return;
        name = (name ?? "").Trim();
        if (name.Length < 3 || name.Length > 10) { SetMessage("Görünen isim 3–10 karakter olmalı."); done?.Invoke(false); return; }
        IsBusy = true; int epoch = generation; SetMessage("İsim kaydediliyor…");
        PlayFabClientAPI.UpdateUserTitleDisplayName(new UpdateUserTitleDisplayNameRequest { DisplayName = name }, result => {
            if (epoch != generation) return; DisplayName = result.DisplayName; PhotonNetwork.NickName = DisplayName; IsBusy = false; SetMessage("İsim kaydedildi."); done?.Invoke(true);
        }, error => { if (epoch != generation) return; IsBusy = false; SetMessage(ErrorMessage(error)); done?.Invoke(false); });
    }
    public void SaveRecoveryEmail(string email, Action<bool> done)
    {
        if (!IsLoggedIn || IsBusy) return;
        email = (email ?? "").Trim();
        if (!Regex.IsMatch(email, @"^[^\s@]+@[^\s@]+\.[^\s@]+$")) { SetMessage("Geçerli bir e-posta adresi yaz."); done?.Invoke(false); return; }
        IsBusy = true; int epoch = generation;
        PlayFabClientAPI.AddOrUpdateContactEmail(new AddOrUpdateContactEmailRequest { EmailAddress = email }, result => {
            if (epoch != generation) return; IsBusy = false; SetMessage("Kurtarma e-postası kaydedildi."); done?.Invoke(true);
        }, error => { if (epoch != generation) return; IsBusy = false; SetMessage(ErrorMessage(error)); done?.Invoke(false); });
    }
    public void Recover(string email, Action<bool> done)
    {
        if (IsBusy) return;
        var config = Resources.Load<OnlineAccountSettings>("OnlineAccountSettings");
        if (config == null || string.IsNullOrWhiteSpace(config.recoveryEmailTemplateId)) { SetMessage("Şifre kurtarma e-posta hizmeti henüz ayarlanmamış."); done?.Invoke(false); return; }
        if (!Regex.IsMatch(email ?? "", @"^[^\s@]+@[^\s@]+\.[^\s@]+$")) { SetMessage("Geçerli bir e-posta adresi yaz."); done?.Invoke(false); return; }
        IsBusy = true; int epoch = generation;
        PlayFabClientAPI.SendAccountRecoveryEmail(new SendAccountRecoveryEmailRequest { Email = email.Trim(), EmailTemplateId = config.recoveryEmailTemplateId, TitleId = PlayFabSettings.staticSettings.TitleId }, result => {
            if (epoch != generation) return; IsBusy = false; SetMessage("Kayıtlı adres için kurtarma e-postası istendi."); done?.Invoke(true);
        }, error => { if (epoch != generation) return; IsBusy = false; SetMessage(ErrorMessage(error)); done?.Invoke(false); });
    }
    public void ConnectPhoton(Action<bool> done)
    {
        if (!IsLoggedIn) { SetMessage("Çevrimiçi oyun için giriş yap."); done?.Invoke(false); return; }
        if (PhotonNetwork.IsConnectedAndReady && PhotonNetwork.InLobby && PhotonNetwork.AuthValues?.UserId == PlayerId) { done?.Invoke(true); return; }
        connectionCallbacks.Add(done); if (connecting) return;
        connecting = true; connectionDeadline = Time.realtimeSinceStartup + 25; int epoch = generation;
        PlayFabClientAPI.GetPhotonAuthenticationToken(new GetPhotonAuthenticationTokenRequest { PhotonApplicationId = PhotonNetwork.PhotonServerSettings.AppSettings.AppIdRealtime }, result => {
            if (epoch != generation) return;
            photonStart = () => {
            if (epoch != generation || !connecting) return;
            PhotonNetwork.AuthValues = new AuthenticationValues(PlayerId) { AuthType = CustomAuthenticationType.Custom };
            PhotonNetwork.AuthValues.AddAuthParameter("username", PlayerId); PhotonNetwork.AuthValues.AddAuthParameter("token", result.PhotonCustomAuthenticationToken);
            PhotonNetwork.PhotonServerSettings.AppSettings.FixedRegion = "eu"; PhotonNetwork.GameVersion = OnlineVersion;
            PhotonNetwork.AutomaticallySyncScene = true; PhotonNetwork.NickName = DisplayName;
            PhotonNetwork.EnableCloseConnection = true;
            if (!PhotonNetwork.IsConnected) { if (!PhotonNetwork.ConnectUsingSettings()) FinishConnection(false, "Sunucu bağlantısı başlatılamadı."); }
            else if (PhotonNetwork.IsConnectedAndReady && !PhotonNetwork.InRoom) PhotonNetwork.JoinLobby();
            };
            if (PhotonNetwork.IsConnected && PhotonNetwork.AuthValues?.UserId != PlayerId) { reconnecting = true; PhotonNetwork.Disconnect(); }
            else photonStart();
        }, error => { if (epoch == generation) FinishConnection(false, ErrorMessage(error)); });
    }
    public override void OnConnectedToMaster() { if (IsLoggedIn && !PhotonNetwork.InLobby) PhotonNetwork.JoinLobby(); }
    public override void OnJoinedLobby() { if (connecting) FinishConnection(true, ""); }
    public override void OnCustomAuthenticationFailed(string debugMessage) => FinishConnection(false, "Oyun sunucusu hesap doğrulamasını kabul etmedi.");
    public override void OnDisconnected(DisconnectCause cause) { if (connecting && reconnecting) { reconnecting = false; photonStart?.Invoke(); } else if (connecting) FinishConnection(false, "Oyun sunucusuyla bağlantı kesildi."); }
    void FinishConnection(bool ok, string message)
    {
        connecting = reconnecting = false; photonStart = null; var callbacks = connectionCallbacks.ToArray(); connectionCallbacks.Clear();
        if (!ok) SetMessage(message); foreach (var callback in callbacks) callback?.Invoke(ok);
    }
    public void Logout()
    {
        if(IsLoggedIn && !string.IsNullOrEmpty(rememberedCustomId))PlayFabClientAPI.UnlinkCustomID(new UnlinkCustomIDRequest{CustomId=rememberedCustomId},_=>{},_=>{});
        RememberedAccountStore.Clear();RememberLogin=IsRestoring=false;rememberedCustomId=null;
        FriendsService.Instance?.Offline(); FriendMatchService.Instance?.Cancel(); generation++;
        PlayerId = Username = DisplayName = null; IsBusy = false;
        FinishConnection(false, "Çıkış yapıldı.");
        PlayFabClientAPI.ForgetAllCredentials();
        if (PhotonNetwork.IsConnected) PhotonNetwork.Disconnect(); PhotonNetwork.AuthValues = null;
        PhotonNetwork.NickName = PlayerPrefs.GetString("PlayerName", "Oyuncu"); Changed?.Invoke();
    }
    public void SetMessage(string value) { Message = value; Changed?.Invoke(); }
    public static string ErrorMessage(PlayFabError error)
    {
        switch (error.Error)
        {
            case PlayFabErrorCode.UsernameNotAvailable: return "Bu kullanıcı adı alınmış.";
            case PlayFabErrorCode.InvalidUsername: return "Kullanıcı adı geçersiz.";
            case PlayFabErrorCode.InvalidPassword: return "Şifre geçersiz.";
            case PlayFabErrorCode.InvalidUsernameOrPassword: return "Kullanıcı adı veya şifre yanlış.";
            case PlayFabErrorCode.AccountNotFound: return "Hesap bulunamadı.";
            case PlayFabErrorCode.NameNotAvailable: return "Bu görünen isim kullanılıyor.";
            case PlayFabErrorCode.InvalidEmailAddress: return "E-posta adresi geçersiz.";
            case PlayFabErrorCode.AccountBanned: return "Bu hesap kullanıma kapalı.";
            case PlayFabErrorCode.NotAuthenticated: return "Oturum sona erdi. Yeniden giriş yap.";
            case PlayFabErrorCode.PhotonApplicationNotFound: return "PlayFab–Photon sunucu bağlantısı henüz yapılandırılmamış.";
            case PlayFabErrorCode.APIClientRequestRateLimitExceeded: return "Çok sık deneme yapıldı. Biraz bekleyip tekrar dene.";
            default: return "İşlem tamamlanamadı. Bağlantını kontrol edip tekrar dene.";
        }
    }
}
