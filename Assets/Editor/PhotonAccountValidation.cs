using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using Photon.Realtime;
using ExitGames.Client.Photon;
using Hashtable = ExitGames.Client.Photon.Hashtable;

// Opt-in live validation: two disposable accounts; credentials never written to disk.
public static class PhotonAccountValidation
{
    // JsonUtility fills response fields; omitted request flags deliberately default to false.
#pragma warning disable CS0649
    [Serializable] class Args { public string action,target,state,requestId,inviteId,host,room,region,decision; public int category; }
    [Serializable] class Request { public string TitleId="13C073",Username,Password,PhotonApplicationId,FunctionName; public bool RequireBothUsernameAndEmail,GeneratePlayStreamEvent; public Args FunctionParameter; }
    [Serializable] class Data { public string PlayFabId,SessionTicket,PhotonCustomAuthenticationToken; public SocialReply FunctionResult; public ScriptError Error; }
    [Serializable] class ScriptError { public string Error,Message; }
    [Serializable] class Envelope { public int code; public string error,errorMessage; public Data data; }
#pragma warning restore CS0649
    class Account { public string id,ticket,token; }
    static readonly List<string> notes=new List<string>();
    static readonly List<LoadBalancingClient> clients=new List<LoadBalancingClient>();
    static IEnumerator work;
    static double deadline;
    static Data last;
    static string networkError;
    static short expectedError;
    static bool expectedReceived;
    const string Output=".utmp/photon-account-validation.txt";
    static string AppId => Photon.Pun.PhotonNetwork.PhotonServerSettings.AppSettings.AppIdRealtime;
    public static void RunBatch()
    {
        Directory.CreateDirectory(".utmp");notes.Clear();clients.Clear();networkError=null;
        expectedError=0;expectedReceived=false;work=Check();deadline=EditorApplication.timeSinceStartup+180;EditorApplication.update+=Advance;
    }
    static void Advance()
    {
        try {
            foreach(var client in clients)client.Service();
            Require(EditorApplication.timeSinceStartup<deadline,"Overall live check timed out.");
            Require(networkError==null,networkError);
            if(work.MoveNext())return;
            Finish(0);
        } catch(Exception error) {notes.Add("FAIL: "+error.Message);Finish(1);}
    }
    static void Finish(int code)
    {
        EditorApplication.update-=Advance;foreach(var client in clients)client.Disconnect();
        File.WriteAllText(Output,string.Join("\n",notes)+"\n");EditorApplication.Exit(code);
    }
    static IEnumerator Send(string method,Request body,Account account=null)
    {
        using(var request=new UnityWebRequest("https://13C073.playfabapi.com/Client/"+method,"POST")) {
            request.uploadHandler=new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonUtility.ToJson(body)));
            request.downloadHandler=new DownloadHandlerBuffer();request.SetRequestHeader("Content-Type","application/json");
            if(account!=null)request.SetRequestHeader("X-Authorization",account.ticket);
            request.timeout=25;var operation=request.SendWebRequest();while(!operation.isDone)yield return null;
            var response=JsonUtility.FromJson<Envelope>(request.downloadHandler.text);
            Require(response!=null && response.code==200,method+": "+(response?.error??request.error));
            last=response.data;
            Require(last?.Error==null || string.IsNullOrEmpty(last.Error.Error),"CloudScript: "+last?.Error?.Error);
        }
    }
    static IEnumerator Social(Account account,Args args)
    {
        var task=Send("ExecuteCloudScript",new Request{FunctionName="Social",FunctionParameter=args},account);
        while(task.MoveNext())yield return task.Current;
        Require(last.FunctionResult!=null && last.FunctionResult.ok,"Social "+args.action+": "+last.FunctionResult?.error);
    }
    static IEnumerator Wait(Func<bool> ready,string label)
    {
        var until=EditorApplication.timeSinceStartup+40;
        while(!ready()){Require(EditorApplication.timeSinceStartup<until,label+" timed out.");yield return null;}
    }
    static IEnumerator Check()
    {
        var accounts=new List<Account>();
        for(int i=0;i<2;i++) {
            var user="netqa"+Guid.NewGuid().ToString("N").Substring(0,10);var password=Guid.NewGuid().ToString("N")+"Aa1!";
            var task=Send("RegisterPlayFabUser",new Request{Username=user,Password=password});while(task.MoveNext())yield return task.Current;
            var account=new Account{id=last.PlayFabId,ticket=last.SessionTicket};accounts.Add(account);
            task=Send("GetPhotonAuthenticationToken",new Request{PhotonApplicationId=AppId},account);while(task.MoveNext())yield return task.Current;
            account.token=last.PhotonCustomAuthenticationToken;
        }
        notes.Add("PASS: two PlayFab accounts registered and Photon tokens issued; credentials remain in memory.");
        var a=accounts[0];var b=accounts[1];var requestId=Guid.NewGuid().ToString("N");
        var step=Social(a,new Args{action="request",target=b.id,requestId=requestId});while(step.MoveNext())yield return step.Current;
        step=Social(b,new Args{action="respond",target=a.id,requestId=requestId,decision="accept"});while(step.MoveNext())yield return step.Current;
        foreach(var account in accounts) {
            step=Social(account,new Args{action="presence",state="menu"});while(step.MoveNext())yield return step.Current;
            var client=new LoadBalancingClient(ConnectionProtocol.Tcp){AuthValues=new AuthenticationValues(account.id){AuthType=CustomAuthenticationType.Custom}};
            client.AuthValues.AddAuthParameter("username",account.id);client.AuthValues.AddAuthParameter("token",account.token);
            client.LoadBalancingPeer.DebugOut=DebugLevel.OFF;
            client.OpResponseReceived+=response=>{if(response.ReturnCode==expectedError && expectedError!=0){expectedReceived=true;return;}if(response.ReturnCode!=0)networkError="Photon operation "+response.OperationCode+" rejected: "+response.ReturnCode;};
            clients.Add(client);
            Require(client.ConnectUsingSettings(new AppSettings{AppIdRealtime=AppId,AppVersion=AccountService.OnlineVersion+".validation",FixedRegion="eu",Protocol=ConnectionProtocol.Tcp,UseNameServer=true,EnableProtocolFallback=true}),"Photon connect could not start.");
        }
        step=Wait(()=>clients.All(c=>c.IsConnectedAndReady),"Two authenticated clients");while(step.MoveNext())yield return step.Current;
        Require(clients[0].UserId==a.id && clients[1].UserId==b.id,"Authenticated Photon user IDs differ from PlayFab.");
        notes.Add("PASS: two independent Photon Realtime clients authenticated with PlayFab IDs in eu.");
        var room="friend-"+Guid.NewGuid().ToString("N");var inviteId=Guid.NewGuid().ToString("N");
        Require(clients[0].OpCreateRoom(new EnterRoomParams{RoomName=room,ExpectedUsers=new[]{b.id},RoomOptions=new RoomOptions{MaxPlayers=2,IsVisible=false,PublishUserId=true,CustomRoomProperties=new Hashtable{{"C",3},{"F",inviteId},{"Host",a.id},{"Guest",b.id}}}}),"Create private room did not start.");
        step=Wait(()=>clients[0].InRoom,"Host private room");while(step.MoveNext())yield return step.Current;
        expectedError=ErrorCode.NoRandomMatchFound;expectedReceived=false;
        Require(clients[1].OpJoinRandomRoom(new OpJoinRandomRoomParams{ExpectedCustomRoomProperties=new Hashtable{{"C",3}},ExpectedMaxPlayers=2}),"Random matchmaking did not start.");
        step=Wait(()=>expectedReceived,"Private room excluded from random matching");while(step.MoveNext())yield return step.Current;
        expectedError=0;notes.Add("PASS: random matchmaking cannot discover the invitation room (NoRandomMatchFound). Validation uses an isolated version suffix.");
        step=Social(a,new Args{action="invite",target=b.id,inviteId=inviteId,room=room,region="eu",category=3});while(step.MoveNext())yield return step.Current;
        step=Social(b,new Args{action="snapshot"});while(step.MoveNext())yield return step.Current;
        Require(last.FunctionResult.snapshot.invite.id==inviteId,"Guest inbox missing real-room invitation.");
        step=Social(b,new Args{action="answerInvite",host=a.id,inviteId=inviteId,decision="accept"});while(step.MoveNext())yield return step.Current;
        Require(clients[1].OpJoinRoom(new EnterRoomParams{RoomName=room}),"Guest join did not start.");
        step=Wait(()=>clients.All(c=>c.InRoom && c.CurrentRoom.PlayerCount==2),"Two-player private room");while(step.MoveNext())yield return step.Current;
        Require(!clients[0].CurrentRoom.IsVisible && clients[0].CurrentRoom.MaxPlayers==2 && (int)clients[1].CurrentRoom.CustomProperties["C"]==3,"Private room/category properties mismatch.");
        Require(clients[0].CurrentRoom.Players.Values.Select(p=>p.UserId).OrderBy(x=>x).SequenceEqual(new[]{a.id,b.id}.OrderBy(x=>x)),"Unexpected reserved-room participant identity.");
        step=Social(a,new Args{action="startMatch",host=a.id,inviteId=inviteId});while(step.MoveNext())yield return step.Current;
        notes.Add("PASS: invitation published after real private room creation; guest inbox/accept/direct join; reserved identities, capacity 2, hidden room and C=3; server invitation started.");
        clients[0].CurrentRoom.IsOpen=false;
        clients[1].OpLeaveRoom(false);step=Wait(()=>!clients[1].InRoom && clients[0].CurrentRoom.PlayerCount==1,"Guest leaving");while(step.MoveNext())yield return step.Current;
        step=Social(a,new Args{action="finishInvite",host=a.id,inviteId=inviteId});while(step.MoveNext())yield return step.Current;
        clients[0].OpLeaveRoom(false);step=Wait(()=>clients.All(c=>!c.InRoom),"Room cleanup");while(step.MoveNext())yield return step.Current;
        step=Social(a,new Args{action="remove",target=b.id});while(step.MoveNext())yield return step.Current;
        foreach(var account in accounts){step=Social(account,new Args{action="presence",state="offline"});while(step.MoveNext())yield return step.Current;}
        notes.Add("PASS: guest departure and private room/invitation cleanup; QA friends removed and presence offline.");
        notes.Add("NOT TESTED: full two game UIs/word RPCs/rematch, physical phones, SMTP delivery. This check uses two real network clients in one isolated Unity Editor process.");
    }
    static void Require(bool condition,string message){if(!condition)throw new Exception(message);}
}
