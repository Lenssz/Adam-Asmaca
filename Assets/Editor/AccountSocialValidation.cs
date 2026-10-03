using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

[InitializeOnLoad]
public static class AccountSocialValidation
{
    const string Key="Paper.AccountValidation";
    const string Output=".utmp/account-validation";
    static IEnumerator work;
    static double deadline;
    static AccountSocialValidation()
    {
        EditorApplication.playModeStateChanged+=state=>{
            if(!SessionState.GetBool(Key,false))return;
            if(state==PlayModeStateChange.EnteredPlayMode){work=Check();deadline=EditorApplication.timeSinceStartup+90;EditorApplication.update+=Advance;}
            if(state==PlayModeStateChange.EnteredEditMode){SessionState.SetBool(Key,false);EditorApplication.Exit(File.Exists(Output+"/error.txt")?1:0);}
        };
    }
    public static void RunBatch()
    {
        Directory.CreateDirectory(Output);if(File.Exists(Output+"/error.txt"))File.Delete(Output+"/error.txt");
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity",OpenSceneMode.Single);
        SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;
    }
    static void Advance()
    {
        try {Require(EditorApplication.timeSinceStartup<deadline,"Account UI test timeout.");if(work.MoveNext())return;}
        catch(Exception error){File.WriteAllText(Output+"/error.txt",error.ToString());Debug.LogException(error);}
        EditorApplication.update-=Advance;EditorApplication.isPlaying=false;
    }
    static IEnumerator Check()
    {
        yield return null;
        var account=AccountService.Instance;var social=FriendsService.Instance;
        Require(account!=null && social!=null && FriendMatchService.Instance!=null,"Persistent services missing.");
        Require(!account.IsLoggedIn,"Launch must be guest without stored credentials.");
        Require(AccountService.ValidateCredentials("ab","123456")!=null && AccountService.ValidateCredentials("abc","12345")!=null,"Credential lower boundaries.");
        Require(AccountService.ValidateCredentials(new string('a',20),new string('b',100))==null,"Valid boundaries rejected.");
        Require(AccountService.ValidateCredentials(new string('a',21),"123456")!=null,"Credential upper boundary.");
        var intro=UnityEngine.Object.FindFirstObjectByType<PaperMenuIntro>();intro.Complete();yield return null;
        var ui=UnityEngine.Object.FindFirstObjectByType<PaperAccountUI>();var lobby=UnityEngine.Object.FindFirstObjectByType<LobbyManager>();
        Require(ui!=null && ui.PlayerIdLabel.text=="Misafir oyuncu","Guest profile UI missing.");
        var target=new RenderTexture(720,1560,24);Bind(target);
        lobby.SelectMultiplayerMode();while(PaperPageTransition.IsTransitioning)yield return null;
        Require(ui.AuthPage.activeSelf && !Photon.Pun.PhotonNetwork.IsConnected,"Guest multiplayer must show login without connecting Photon.");
        Require(!ui.RememberMeSelected,"Remember choice must be opt-in.");
        ui.RememberButton.onClick.Invoke();Require(ui.RememberMeSelected,"Remember selection failed.");
        ui.RememberButton.onClick.Invoke();Require(!ui.RememberMeSelected,"Remember deselection failed.");
        Capture(target,"login-portrait");
        ui.UsernameInput.text="ab";ui.PasswordInput.text="secret123";Button(ui.AuthPage,"Giriş yap").onClick.Invoke();
        Require(!account.IsLoggedIn && !account.IsBusy && ui.PasswordInput.text=="","Invalid input must clear password and release operation lock.");
        Button(ui.AuthPage,"Yeni hesap oluştur").onClick.Invoke();Capture(target,"register-portrait");
        var snapshot=new SocialSnapshot {
            friends=new System.Collections.Generic.List<SocialPlayer> {new SocialPlayer{id="ABC12345",username="bora",name="Bora",online=true,available=true},new SocialPlayer{id="DEF12345",username="ayse",name="Ayşe",online=true,available=false},new SocialPlayer{id="AAA12345",username="cem",name="Cem",online=false}},
            incoming=new System.Collections.Generic.List<SocialRequest>{new SocialRequest{id="r1",from="BBB12345",to="FA123456",player=new SocialPlayer{id="BBB12345",name="Çizgi"}}},
            outgoing=new System.Collections.Generic.List<SocialRequest>{new SocialRequest{id="r2",from="FA123456",to="CCC12345",player=new SocialPlayer{id="CCC12345",name="Kalem"}}}
        };
        social.ValidationTransport=(action,args)=>action=="snapshot"?new SocialReply{ok=true,snapshot=snapshot}:new SocialReply{ok=true};
        account.ValidationLogin("FA123456","kalemtest","Çizgi");account.SetMessage("");social.Refresh();
        Button(ui.AuthPage,"Geri").onClick.Invoke();while(PaperPageTransition.IsTransitioning)yield return null;
        lobby.OpenProfile();while(PaperPageTransition.IsTransitioning)yield return null;
        Require(ui.PlayerIdLabel.text.Contains("FA123456") && lobby.currentNameText.text=="Çizgi","Account ID and server display name missing.");
        Capture(target,"profile-portrait");
        Button(lobby.profilePanel,"Arkadaşlar").onClick.Invoke();while(PaperPageTransition.IsTransitioning)yield return null;
        Require(ui.FriendsPage.activeSelf,"Friends page failed.");Capture(target,"friends-portrait");
        var all=ui.FriendsPage.GetComponentsInChildren<Button>();var invitations=all.Where(b=>b.GetComponentInChildren<TMP_Text>()?.text=="1v1").ToArray();
        Require(invitations.Length==3 && invitations.Count(b=>b.interactable)==1,"Only available friend may receive 1v1 invitation.");
        Button(ui.FriendsPage,"Gelen").onClick.Invoke();Capture(target,"requests-portrait");
        Require(ui.FriendsPage.GetComponentsInChildren<TMP_Text>().Any(t=>t.text=="Çizgi"),"Incoming request missing.");
        Button(ui.FriendsPage,"Gönderilen").onClick.Invoke();Require(ui.FriendsPage.GetComponentsInChildren<TMP_Text>().Any(t=>t.text=="Kalem"),"Outgoing request missing.");
        Button(ui.FriendsPage,"Liste").onClick.Invoke();
        var wide=new RenderTexture(1080,1920,24);Bind(wide);yield return null;Capture(wide,"friends-9x16");
        Button(ui.FriendsPage,"Geri").onClick.Invoke();while(PaperPageTransition.IsTransitioning)yield return null;Capture(wide,"profile-9x16");
        Button(lobby.profilePanel,"Kurtarma e-postası").onClick.Invoke();while(PaperPageTransition.IsTransitioning)yield return null;Require(ui.EmailPage.activeSelf,"Email panel missing.");Capture(wide,"email-9x16");
        Button(ui.EmailPage,"Geri").onClick.Invoke();while(PaperPageTransition.IsTransitioning)yield return null;
        GameManager.Instance.SelectCategory(3);while(PaperPageTransition.IsTransitioning){Bind(wide);yield return null;}
        Require(SceneManager.GetActiveScene().name=="GAMESCENE" && AccountService.Instance==account && account.PlayerId=="FA123456","Account did not persist into single player.");
        Require(GameManager.Instance.WrongGuessCount==0 && !UnityEngine.Object.FindFirstObjectByType<HangmanDrawer>().IsDrawing,"Account entry triggered body drawing.");
        GameManager.Instance.GoToMainMenu();while(PaperPageTransition.IsTransitioning){Bind(wide);yield return null;}
        Require(AccountService.Instance==account && account.IsLoggedIn && UnityEngine.Object.FindFirstObjectByType<PaperAccountUI>()!=null,"Account/menu UI not preserved on return.");
        account.Logout();Require(!account.IsLoggedIn && social.Snapshot.friends.Count==0,"Logout must clear friends and credentials.");
        social.ValidationTransport=null;Bind(null);target.Release();wide.Release();UnityEngine.Object.Destroy(target);UnityEngine.Object.Destroy(wide);
        File.WriteAllText(Output+"/results.txt","PASS: default guest; credential boundaries; multiplayer login gate; invalid login cleanup; login/register/profile/ID/friends/incoming/outgoing/recovery UI at 720x1560 and 1080x1920; only available friends inviteable; account persists across real game/menu scene changes; no body part triggered; logout clears social state. Social UI uses Editor-only injected responses. Live PlayFab/Photon/email/mobile validation reported separately.\n");
    }
    static Button Button(GameObject root,string text)=>root.GetComponentsInChildren<Button>().First(b=>b.GetComponentInChildren<TMP_Text>()?.text==text);
    static void Bind(RenderTexture target){if(Camera.main!=null)Camera.main.targetTexture=target;}
    static void Capture(RenderTexture target,string name)
    {
        Bind(target);Canvas.ForceUpdateCanvases();Camera.main.Render();var previous=RenderTexture.active;RenderTexture.active=target;
        var texture=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,target.width,target.height),0,0);texture.Apply();File.WriteAllBytes(Output+"/"+name+".png",texture.EncodeToPNG());UnityEngine.Object.Destroy(texture);RenderTexture.active=previous;
    }
    static void Require(bool condition,string message){if(!condition)throw new Exception(message);}
}
