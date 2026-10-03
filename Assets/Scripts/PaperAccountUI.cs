using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PaperAccountUI : MonoBehaviour
{
    public GameObject AuthPage { get; private set; }
    public GameObject FriendsPage { get; private set; }
    public GameObject EmailPage { get; private set; }
    public GameObject MatchPage { get; private set; }
    public TMP_InputField UsernameInput { get; private set; }
    public TMP_InputField PasswordInput { get; private set; }
    public TMP_InputField SearchInput { get; private set; }
    public TMP_Text PlayerIdLabel { get; private set; }
    public TMP_Text StatusLabel { get; private set; }
    LobbyManager lobby;
    TMP_FontAsset font;
    Sprite outline;
    Transform safe;
    TMP_InputField emailInput;
    TMP_Text authHeading, authNote, emailHeading, emailNote, searchResult, friendStatus, matchStatus, profileStatus, toastText, menuStatus;
    Button submit, toggle, idCopy, profileFriends, recovery, logout, searchAdd, emailSubmit, retryRemember;
    public Button RememberButton { get; private set; }
    public bool RememberMeSelected { get; private set; }
    Transform rows;
    GameObject toast, categoryPicker;
    readonly List<Button> commands = new List<Button>();
    bool register, onlineAfterLogin, emailSave;
    string tab = "friends", seenInvite;
    bool accountLocked;
    bool restoreAuthPending;
    SocialPlayer found, chosenFriend;
    static readonly Color Ink = new Color(.19f,.18f,.17f);
    static readonly string[] Categories = { "League of Legends", "Valorant", "Counter-Strike", "Genel Kültür", "Ülkeler", "Minecraft" };
    void Awake()
    {
        lobby = GetComponent<LobbyManager>();
        safe = lobby.modeSelectionPanel.transform.parent;
        font = lobby.currentNameText.font;
        outline = lobby.modeSelectionPanel.GetComponentInChildren<Button>(true).GetComponent<Image>().sprite;
        Build();
        menuStatus = Label(lobby.modeSelectionPanel.transform,"",.055f,27,75);
    }
    void OnEnable()
    {
        AccountService.Instance.Changed += RefreshAccount;
        FriendsService.Instance.Changed += RefreshFriends;
        FriendMatchService.Instance.Changed += RefreshMatch;
        RefreshAccount(); RefreshFriends();
    }
    void OnDisable()
    {
        if (AccountService.Instance != null) AccountService.Instance.Changed -= RefreshAccount;
        if (FriendsService.Instance != null) FriendsService.Instance.Changed -= RefreshFriends;
        if (FriendMatchService.Instance != null) FriendMatchService.Instance.Changed -= RefreshMatch;
        if (PasswordInput != null) PasswordInput.text = "";
    }
    void Update()
    {
        if (accountLocked != AccountService.Instance.IsBusy) { accountLocked = AccountService.Instance.IsBusy; RefreshAccount(); }
        if(restoreAuthPending && AccountService.Instance.IsLoggedIn && AuthPage.activeSelf && !PaperPageTransition.IsTransitioning){restoreAuthPending=false;Return(onlineAfterLogin?lobby.modeSelectionPanel:lobby.profilePanel);if(onlineAfterLogin)StartCoroutine(ContinueOnline());}
        if (MatchPage.activeSelf && !FriendMatchService.Instance.IsBusy && !PaperPageTransition.IsTransitioning) Return(FriendsPage);
    }
    void Build()
    {
        AuthPage = Page("Account login page");
        authHeading = Label(AuthPage.transform,"Hesabına giriş yap",.9f,62);
        UsernameInput = Input(AuthPage.transform,"Kullanıcı adı",.76f,20); UsernameInput.text = AccountService.Instance.RememberedUsername;
        PasswordInput = Input(AuthPage.transform,"Şifre",.64f,100,true);
        RememberButton=Button(AuthPage.transform,"[ ] Beni hatırla",.535f,()=>{RememberMeSelected=!RememberMeSelected;RefreshAccount();},65);
        authNote = Label(AuthPage.transform,"Çevrimiçi oyun ve arkadaşlar için giriş yap.",.425f,29,105);
        submit = Button(AuthPage.transform,"Giriş yap",.32f,Authenticate);
        toggle = Button(AuthPage.transform,"Hesap oluştur",.20f,() => { register = !register; PasswordInput.text = ""; RefreshAccount(); });
        Button(AuthPage.transform,"Şifremi unuttum",.09f,() => OpenEmail(false));
        Button(AuthPage.transform,"Geri",.01f,() => Return(lobby.modeSelectionPanel),75);
        retryRemember=Button(lobby.modeSelectionPanel.transform,"Otomatik girişi tekrar dene",.13f,()=>AccountService.Instance.RestoreRememberedAccount(),70);retryRemember.gameObject.SetActive(false);

        FriendsPage = Page("Account friends page");
        Label(FriendsPage.transform,"Arkadaşlar",.94f,58);
        SearchInput = Input(FriendsPage.transform,"Oyuncu ID veya kullanıcı adı",.83f,32);
        Button(FriendsPage.transform,"Ara",.73f,() => FriendsService.Instance.Search(SearchInput.text, player => { found = player; searchResult.text = player == null ? FriendsService.Instance.Message : player.name + " · " + player.id; searchAdd.interactable = player != null; }));
        searchResult = Label(FriendsPage.transform,"",.645f,29,60);
        searchAdd = Button(FriendsPage.transform,"Arkadaşlık isteği gönder",.565f,() => { if (found != null) FriendsService.Instance.SendRequest(found.id); },80); searchAdd.interactable=false;
        var tabs = new GameObject("Friend tabs",typeof(RectTransform)).transform; tabs.SetParent(FriendsPage.transform,false); Position((RectTransform)tabs,.02f,.46f,.98f,.46f,90);
        Tab(tabs,"Liste","friends",0); Tab(tabs,"Gelen","incoming",1); Tab(tabs,"Gönderilen","outgoing",2);
        friendStatus = Label(FriendsPage.transform,"",.365f,28,90);
        var scrollGo = new GameObject("Friends scroll",typeof(RectTransform),typeof(Image),typeof(ScrollRect)); scrollGo.transform.SetParent(FriendsPage.transform,false);
        Position((RectTransform)scrollGo.transform,.01f,.08f,.99f,.32f,0); scrollGo.GetComponent<Image>().color=Color.clear;
        var viewport = new GameObject("Viewport",typeof(RectTransform),typeof(RectMask2D));viewport.transform.SetParent(scrollGo.transform,false);Stretch((RectTransform)viewport.transform);
        var content = new GameObject("Rows",typeof(RectTransform),typeof(VerticalLayoutGroup),typeof(ContentSizeFitter));content.transform.SetParent(viewport.transform,false); rows=content.transform;
        var rect=(RectTransform)rows;rect.anchorMin=new Vector2(0,1);rect.anchorMax=Vector2.one;rect.pivot=new Vector2(.5f,1);rect.sizeDelta=Vector2.zero;
        var layout=content.GetComponent<VerticalLayoutGroup>();layout.spacing=12;layout.childControlHeight=true;layout.childControlWidth=true;layout.childForceExpandHeight=false;
        content.GetComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
        var scroll=scrollGo.GetComponent<ScrollRect>();scroll.viewport=(RectTransform)viewport.transform;scroll.content=rect;scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;
        Button(FriendsPage.transform,"Geri",.025f,() => Return(lobby.profilePanel),75);

        EmailPage=Page("Account recovery page");emailHeading=Label(EmailPage.transform,"Kurtarma e-postası",.86f,54);
        emailInput=Input(EmailPage.transform,"E-posta adresi",.62f,254);emailInput.keyboardType=TouchScreenKeyboardType.EmailAddress;
        emailNote=Label(EmailPage.transform,"",.44f,32,180);
        emailSubmit=Button(EmailPage.transform,"Kaydet",.26f,() => {
            if(emailSave)AccountService.Instance.SaveRecoveryEmail(emailInput.text,ok=>{emailNote.text=AccountService.Instance.Message;});
            else AccountService.Instance.Recover(emailInput.text,ok=>{emailNote.text=AccountService.Instance.Message;});
        });
        Button(EmailPage.transform,"Geri",.1f,() => Return(emailSave?lobby.profilePanel:AuthPage));

        MatchPage=Page("Friend match waiting page");Label(MatchPage.transform,"Arkadaşla 1v1",.84f,56);
        matchStatus=Label(MatchPage.transform,"",.55f,40,230);
        Button(MatchPage.transform,"İptal et",.2f,() => { FriendMatchService.Instance.Cancel();Return(FriendsPage); });
        categoryPicker=Page("Friend match categories");Label(categoryPicker.transform,"Kategori seç",.9f,56);
        for(int i=0;i<6;i++){int index=i;Button(categoryPicker.transform,Categories[i],.76f-i*.105f,()=>{ if(chosenFriend==null)return;Show(MatchPage);FriendMatchService.Instance.Invite(chosenFriend,index); },95);}
        Button(categoryPicker.transform,"Geri",.07f,()=>Return(FriendsPage),80);

        var profileRect=lobby.profilePanel.GetComponent<RectTransform>();Position(profileRect,.05f,.08f,.95f,.79f,0);
        var heading=lobby.profilePanel.transform.Find("Profile heading");if(heading!=null) Position((RectTransform)heading,.03f,.94f,.97f,.94f,90);
        Position(lobby.currentNameText.rectTransform,.03f,.835f,.97f,.835f,75);lobby.currentNameText.margin=Vector4.zero;lobby.currentNameText.alignment=TextAlignmentOptions.Center;
        PlayerIdLabel=Label(lobby.profilePanel.transform,"Oyuncu ID",.75f,28,75);
        idCopy=Button(lobby.profilePanel.transform,"ID'yi kopyala",.67f,()=> { if(AccountService.Instance.IsLoggedIn){GUIUtility.systemCopyBuffer=AccountService.Instance.PlayerId;profileStatus.text="Oyuncu ID kopyalandı.";} },80);
        profileFriends=Button(lobby.profilePanel.transform,"Arkadaşlar",.555f,()=> { if(AccountService.Instance.IsLoggedIn){Show(FriendsPage);FriendsService.Instance.Refresh();}else OpenAuth(false); },105);
        Position(lobby.nameInput.GetComponent<RectTransform>(),.08f,.425f,.92f,.425f,110);lobby.nameInput.characterLimit=10;
        foreach(var button in lobby.profilePanel.GetComponentsInChildren<Button>(true))if(button.name=="SaveButton")Position(button.GetComponent<RectTransform>(),.12f,.31f,.88f,.31f,105);
        profileStatus=Label(lobby.profilePanel.transform,"",.225f,28,65);
        recovery=Button(lobby.profilePanel.transform,"Kurtarma e-postası",.145f,()=>OpenEmail(true),80);
        logout=Button(lobby.profilePanel.transform,"Hesaptan çıkış yap",.05f,()=>{AccountService.Instance.Logout();Return(lobby.profilePanel);},80);

        toast=new GameObject("Friend invitation notice",typeof(RectTransform),typeof(Image));toast.transform.SetParent(safe,false);
        Position(toast.GetComponent<RectTransform>(),.04f,.26f,.96f,.57f,0);toast.GetComponent<Image>().sprite=outline;toast.GetComponent<Image>().type=Image.Type.Sliced;toast.GetComponent<Image>().color=new Color(.96f,.94f,.88f,1);
        toastText=Label(toast.transform,"",.72f,37,170);
        Button(toast.transform,"Kabul et",.34f,()=> { var invite=FriendsService.Instance.Snapshot.invite;toast.SetActive(false);Show(MatchPage);FriendMatchService.Instance.Accept(invite); });
        Button(toast.transform,"Reddet",.12f,()=> { var invite=FriendsService.Instance.Snapshot.invite;toast.SetActive(false);FriendMatchService.Instance.Decline(invite); });
        toast.SetActive(false);
    }
    void Tab(Transform parent,string label,string value,int index)
    {
        var b=Button(parent,label,.5f,()=>{tab=value;RefreshFriends();},80);
        Position(b.GetComponent<RectTransform>(),index/3f+.01f,.5f,(index+1)/3f-.01f,.5f,80);
    }
    void Authenticate()
    {
        AccountService.Instance.Authenticate(UsernameInput.text,PasswordInput.text,register,ok=>{
            PasswordInput.text="";if(!ok)return;
            if(onlineAfterLogin){Return(lobby.modeSelectionPanel);StartCoroutine(ContinueOnline());}
            else Return(lobby.profilePanel);
        },RememberMeSelected);
    }
    System.Collections.IEnumerator ContinueOnline() { while(PaperPageTransition.IsTransitioning)yield return null;lobby.SelectMultiplayerMode(); }
    public void OpenAuth(bool online)
    {
        onlineAfterLogin=online;register=false;restoreAuthPending=AccountService.Instance.IsRestoring;RememberMeSelected=AccountService.Instance.RememberLogin;PasswordInput.text="";UsernameInput.text=AccountService.Instance.RememberedUsername;RefreshAccount();Show(AuthPage);
    }
    void OpenEmail(bool save)
    {
        emailSave=save;emailInput.text="";emailHeading.text=save?"Kurtarma e-postası":"Şifremi unuttum";
        emailNote.text=save?"İsteğe bağlı: şifreni unutursan bu adresi kullanırsın.":"Profiline eklediğin kurtarma e-postasını yaz.";
        emailSubmit.GetComponentInChildren<TMP_Text>().text=save?"Kaydet":"Kurtarma e-postası gönder";Show(EmailPage);
    }
    public void HidePages()
    {
        foreach(var page in new[]{AuthPage,FriendsPage,EmailPage,MatchPage,categoryPicker})if(page!=null)page.SetActive(false);
        if(PasswordInput!=null)PasswordInput.text="";
    }
    void Show(GameObject page)
    {
        if(PaperPageTransition.IsTransitioning)return;
        PaperPageTransition.ShowPanel(()=>{
            if(this==null)return;
            foreach(var panel in new[]{lobby.loginPanel,lobby.modeSelectionPanel,lobby.categoryPanel,lobby.waitingPanel,lobby.profilePanel})panel.SetActive(false);
            HidePages();toast.SetActive(false);page.SetActive(true);
        },PaperPageTransition.Instance!=null?PaperPageTransition.Instance.categoryEntryDuration:.7f);
    }
    void Return(GameObject page){RefreshAccount();Show(page);}
    void RefreshAccount()
    {
        var account=AccountService.Instance;
        authHeading.text=register?"Hesap oluştur":"Hesabına giriş yap";submit.GetComponentInChildren<TMP_Text>().text=register?"Hesap oluştur":"Giriş yap";
        toggle.GetComponentInChildren<TMP_Text>().text=register?"Zaten hesabım var":"Yeni hesap oluştur";
        submit.interactable=toggle.interactable=!account.IsBusy;
        RememberButton.interactable=RememberedAccountStore.Supported&&!account.IsBusy;
        RememberButton.GetComponentInChildren<TMP_Text>().text=(RememberMeSelected?"[X]":"[ ]")+" Beni hatırla";
        retryRemember.gameObject.SetActive(account.RememberLogin&&!account.IsLoggedIn&&!account.IsBusy);
        UsernameInput.interactable=PasswordInput.interactable=!account.IsBusy;
        authNote.text=string.IsNullOrEmpty(account.Message)?(RememberedAccountStore.Supported?"Beni hatırla: bu cihazda bir sonraki açılışta otomatik giriş.":"Bu platformda hesap hatırlama desteklenmiyor."):account.Message;
        emailSubmit.interactable=!account.IsBusy;emailInput.interactable=!account.IsBusy;
        PlayerIdLabel.text=account.IsLoggedIn?"Oyuncu ID: "+account.PlayerId:"Misafir oyuncu";
        idCopy.interactable=account.IsLoggedIn;profileFriends.GetComponentInChildren<TMP_Text>().text=account.IsLoggedIn?"Arkadaşlar":"Hesap oluştur / giriş yap";
        recovery.gameObject.SetActive(account.IsLoggedIn);logout.gameObject.SetActive(account.IsLoggedIn);
        lobby.currentNameText.text=account.IsLoggedIn?account.DisplayName:Photon.Pun.PhotonNetwork.NickName;
        if(!lobby.nameInput.isFocused)lobby.nameInput.text=lobby.currentNameText.text;
        if(profileStatus!=null)profileStatus.text=account.Message;
        if(menuStatus!=null)menuStatus.text=account.Message;
    }
    void RefreshFriends()
    {
        if(rows==null)return;
        foreach(Transform child in rows){child.gameObject.SetActive(false);Destroy(child.gameObject);}
        int oldRows=rows.childCount;
        var social=FriendsService.Instance;friendStatus.text=social.Message;
        foreach(var command in commands)if(command!=null)command.interactable=!social.IsBusy; commands.RemoveAll(b=>b==null);
        if(tab=="friends")
        {
            foreach(var friend in social.Snapshot.friends)
            {
                var person=friend;var row=Row(person.name+"\n"+(person.online?(person.available?"Çevrimiçi":"Meşgul"):"Çevrimdışı"));
                var invite=RowButton(row,"1v1",.55f,.79f,()=>{chosenFriend=person;Show(categoryPicker);});invite.interactable=person.available&&!social.IsBusy&&!FriendMatchService.Instance.IsBusy;
                RowButton(row,"Çıkar",.81f,.99f,()=>social.Remove(person.id));
            }
        }
        else foreach(var request in tab=="incoming"?social.Snapshot.incoming:social.Snapshot.outgoing)
        {
            var item=request;var row=Row(item.player.name);
            if(tab=="incoming"){RowButton(row,"Kabul",.55f,.77f,()=>social.Respond(item,"accept"));RowButton(row,"Reddet",.79f,.99f,()=>social.Respond(item,"reject"));}
            else RowButton(row,"İptal",.65f,.99f,()=>social.Respond(item,"cancel"));
        }
        if(rows.childCount==oldRows)Row("Bu bölüm şimdilik boş.");
        var incoming=social.Snapshot.invite;
        if(incoming!=null && incoming.status=="pending" && incoming.to==AccountService.Instance.PlayerId && !FriendMatchService.Instance.IsBusy && social.Presence=="menu" && !PaperPageTransition.IsTransitioning)
        {
            if(seenInvite!=incoming.id){seenInvite=incoming.id;toastText.text=(incoming.player?.name??"Arkadaşın")+" seni 1v1'e davet etti.\n"+Categories[Mathf.Clamp(incoming.category,0,5)];toast.SetActive(true);toast.transform.SetAsLastSibling();}
        }
        else toast.SetActive(false);
    }
    void RefreshMatch()
    {
        matchStatus.text=FriendMatchService.Instance.Message;
        if(MatchPage.activeSelf && !FriendMatchService.Instance.IsBusy && !PaperPageTransition.IsTransitioning)Return(FriendsPage);
    }
    Transform Row(string text)
    {
        var go=new GameObject("Friend row",typeof(RectTransform),typeof(LayoutElement));go.transform.SetParent(rows,false);go.GetComponent<LayoutElement>().preferredHeight=125;
        var label=Label(go.transform,text,.5f,29,110);Position(label.rectTransform,.01f,.5f,.53f,.5f,110);label.alignment=TextAlignmentOptions.MidlineLeft;return go.transform;
    }
    Button RowButton(Transform row,string title,float left,float right,Action click)
    {
        var b=Button(row,title,.5f,click,90);Position(b.GetComponent<RectTransform>(),left,.5f,right,.5f,90);commands.Add(b);b.interactable=!FriendsService.Instance.IsBusy;return b;
    }
    GameObject Page(string name)
    {
        var page=new GameObject(name,typeof(RectTransform));page.transform.SetParent(safe,false);Position(page.GetComponent<RectTransform>(),.06f,.08f,.94f,.80f,0);page.SetActive(false);return page;
    }
    TMP_Text Label(Transform parent,string content,float y,float size,float height=100)
    {
        var go=new GameObject("Paper label",typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(parent,false);var text=go.GetComponent<TMP_Text>();
        Position(text.rectTransform,.025f,y,.975f,y,height);text.font=font;text.fontSize=size;text.enableAutoSizing=true;text.fontSizeMin=Mathf.Max(20,size*.65f);text.fontSizeMax=size;
        text.text=content;text.color=Ink;text.alignment=TextAlignmentOptions.Center;text.margin=Vector4.zero;text.raycastTarget=false;return text;
    }
    Button Button(Transform parent,string content,float y,Action action,float height=110)
    {
        var go=new GameObject("Paper button "+content,typeof(RectTransform),typeof(Image),typeof(Button),typeof(PaperPressFeedback));go.transform.SetParent(parent,false);Position(go.GetComponent<RectTransform>(),.08f,y,.92f,y,height);
        var image=go.GetComponent<Image>();image.sprite=outline;image.type=Image.Type.Sliced;image.color=Color.white;
        var button=go.GetComponent<Button>();button.targetGraphic=image;var colors=button.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(.84f,.84f,.84f);colors.pressedColor=new Color(.60f,.60f,.60f);button.colors=colors;
        var label=Label(go.transform,content,.5f,35,height-10);button.onClick.AddListener(()=>{if(button.IsInteractable()&&!PaperPageTransition.IsTransitioning)action();});return button;
    }
    TMP_InputField Input(Transform parent,string hint,float y,int limit,bool secret=false)
    {
        var go=new GameObject("Paper input "+hint,typeof(RectTransform),typeof(Image),typeof(TMP_InputField));go.transform.SetParent(parent,false);Position(go.GetComponent<RectTransform>(),.04f,y,.96f,y,115);
        go.GetComponent<Image>().sprite=outline;go.GetComponent<Image>().type=Image.Type.Sliced;
        var viewport=new GameObject("Text area",typeof(RectTransform),typeof(RectMask2D));viewport.transform.SetParent(go.transform,false);Stretch((RectTransform)viewport.transform);((RectTransform)viewport.transform).offsetMin=new Vector2(24,8);((RectTransform)viewport.transform).offsetMax=new Vector2(-24,-8);
        var field=go.GetComponent<TMP_InputField>();var label=Label(viewport.transform,"",.5f,38,95);label.enableAutoSizing=false;label.alignment=TextAlignmentOptions.MidlineLeft;
        var placeholder=Label(viewport.transform,hint,.5f,34,95);placeholder.color=new Color(.4f,.38f,.35f);placeholder.alignment=TextAlignmentOptions.MidlineLeft;
        field.textViewport=(RectTransform)viewport.transform;field.textComponent=(TMP_Text)label;field.placeholder=placeholder;field.fontAsset=font;field.characterLimit=limit;field.customCaretColor=true;field.caretColor=Ink;field.selectionColor=new Color(.68f,.77f,.64f,.5f);
        field.contentType=secret?TMP_InputField.ContentType.Password:TMP_InputField.ContentType.Standard;field.lineType=TMP_InputField.LineType.SingleLine;return field;
    }
    static void Position(RectTransform rect,float x1,float y1,float x2,float y2,float height)
    {rect.anchorMin=new Vector2(x1,y1);rect.anchorMax=new Vector2(x2,y2);rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=Vector2.zero;rect.sizeDelta=new Vector2(0,height);rect.localScale=Vector3.one;}
    static void Stretch(RectTransform rect){rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;}
}
