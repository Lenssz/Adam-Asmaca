using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using PlayFab;
using PlayFab.ClientModels;

[InitializeOnLoad]
public static class RememberLoginValidation
{
    const string Flag="Paper.RememberValidation",Output=".utmp/remember-login-validation.txt";
    static IEnumerator work;static double deadline;static string result;
    static RememberLoginValidation()
    {
        EditorApplication.playModeStateChanged+=state=>{
            if(!SessionState.GetBool(Flag,false))return;
            if(state==PlayModeStateChange.EnteredPlayMode){work=Check();deadline=EditorApplication.timeSinceStartup+100;EditorApplication.update+=Advance;}
            if(state==PlayModeStateChange.EnteredEditMode){SessionState.SetBool(Flag,false);File.WriteAllText(Output,result);EditorApplication.Exit(result.StartsWith("PASS")?0:1);}
        };
    }
    public static void RunBatch()
    {
        if(Application.productName!="PaperInteractionValidation")throw new Exception("Live validation must use the isolated validation project.");
        RememberedAccountStore.Clear();Directory.CreateDirectory(".utmp");EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");SessionState.SetBool(Flag,true);EditorApplication.isPlaying=true;
    }
    static void Advance()
    {
        try{Require(EditorApplication.timeSinceStartup<deadline,"Remember validation timeout.");if(work.MoveNext())return;}
        catch(Exception error){result="FAIL: "+error.Message;}
        EditorApplication.update-=Advance;EditorApplication.isPlaying=false;
    }
    static IEnumerator Check()
    {
        yield return null;
        var account=AccountService.Instance;Require(!account.IsLoggedIn,"Initial run must be guest.");
        UnityEngine.Object.FindFirstObjectByType<PaperMenuIntro>().Complete();
        var username="rememberqa"+Guid.NewGuid().ToString("N").Substring(0,8);var password=Guid.NewGuid().ToString("N")+"Aa1!";
        bool complete=false,success=false;
        account.Authenticate(username,password,true,ok=>{success=ok;complete=true;},true);while(!complete)yield return null;
        Require(success && account.RememberLogin,"Live registration/remember link failed: "+account.Message);
        Require(RememberedAccountStore.TryRead(out var saved) && saved.playerId==account.PlayerId,"Protected credential missing.");
        string expectedId=account.PlayerId,customId=saved.customId;
        string path=Path.Combine(Application.persistentDataPath,"paper-remember-13C073.bin");
        var disk=Encoding.UTF8.GetString(File.ReadAllBytes(path));Require(!disk.Contains(customId)&&!disk.Contains(password)&&!disk.Contains(username),"Protected file exposed plaintext credential.");
        var root=account.gameObject;var ui=UnityEngine.Object.FindFirstObjectByType<PaperAccountUI>();UnityEngine.Object.Destroy(ui);UnityEngine.Object.Destroy(root);yield return null;yield return null;
        PlayFabClientAPI.ForgetAllCredentials();
        var restarted=new GameObject("Restarted account validation");UnityEngine.Object.DontDestroyOnLoad(restarted);
        account=restarted.AddComponent<AccountService>();restarted.AddComponent<FriendsService>();restarted.AddComponent<FriendMatchService>();
        yield return null;while(account.IsBusy)yield return null;
        Require(account.IsLoggedIn && account.PlayerId==expectedId && account.Username==username && account.RememberLogin,"Fresh service startup failed to restore existing account: "+account.Message);
        SceneManager.LoadScene("SampleScene");yield return null;yield return null;
        Require(UnityEngine.Object.FindFirstObjectByType<PaperAccountUI>().PlayerIdLabel.text.Contains(expectedId),"Restored profile ID missing.");
        account.Logout();Require(!RememberedAccountStore.TryRead(out _)&&!account.IsLoggedIn,"Logout did not erase protected login.");
        yield return new WaitForSecondsRealtime(2);
        complete=false;bool rejected=false;
        PlayFabClientAPI.LoginWithCustomID(new LoginWithCustomIDRequest{CustomId=customId,CreateAccount=false},_=>complete=true,error=>{rejected=error.Error==PlayFabErrorCode.AccountNotFound;complete=true;});while(!complete)yield return null;
        Require(rejected,"Logout did not unlink the remembered ID on live PlayFab.");
        complete=false;account.Authenticate(username,password,false,ok=>{success=ok;complete=true;},false);while(!complete)yield return null;
        Require(success&&!account.RememberLogin&&!RememberedAccountStore.TryRead(out _),"Unselected remember must not persist credential.");account.Logout();
        RememberedAccountStore.Save(new RememberedAccountStore.Record{customId="remember-"+new string('a',64),playerId=expectedId,username=username});
        account.RestoreRememberedAccount();while(account.IsBusy)yield return null;
        Require(!account.IsLoggedIn&&!account.RememberLogin&&!RememberedAccountStore.TryRead(out _),"Revoked/unknown remembered credential must be cleared without creating account.");
        result="PASS: live register + link random credential; Windows DPAPI ciphertext contains no password/username/CustomId plaintext; fresh AccountService startup logs into SAME account without password; restored profile ID; logout clears protected storage and live link; unchecked option creates no persistent credential; invalid credential cleared with CreateAccount=false. Credentials never logged. Android Java compiled separately; physical Android Keystore/restart not tested.\n";
    }
    static void Require(bool condition,string message){if(!condition)throw new Exception(message);}
}
