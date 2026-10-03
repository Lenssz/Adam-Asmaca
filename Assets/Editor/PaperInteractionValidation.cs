using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public static class PaperInteractionValidation
{
    const string Output=PaperMenuBuilder.Output;
    public static IEnumerator Check(RenderTexture target)
    {
        var lobby=UnityEngine.Object.FindFirstObjectByType<LobbyManager>();
        var button=lobby.modeSelectionPanel.GetComponentsInChildren<Button>().First(b=>b.GetComponentInChildren<TMP_Text>().text=="Tek Oyunculu");
        var feedback=button.GetComponent<PaperPressFeedback>();Require(feedback!=null,"Menu is missing press feedback.");
        var rect=button.GetComponent<RectTransform>();var point=rect.rect.center+new Vector2(rect.rect.width*.35f,rect.rect.height*.18f);
        var canvas=button.GetComponentInParent<Canvas>();
        var data=new PointerEventData(EventSystem.current){pointerId=37,position=RectTransformUtility.WorldToScreenPoint(Camera.main,rect.TransformPoint(point)),eligibleForClick=true,pressPosition=Vector2.zero};
        data.pointerPressRaycast=new RaycastResult{module=canvas.GetComponent<GraphicRaycaster>()};
        ExecuteEvents.Execute(button.gameObject,data,ExecuteEvents.pointerDownHandler);
        Require(Vector2.Distance(feedback.ContactPoint,point)<.1f,"Crease must use the actual finger position.");
        float until=Time.realtimeSinceStartup+.12f;while(Time.realtimeSinceStartup<until)yield return null;
        Require(feedback.Amount>.95f && button.transform.localScale.x<1,"Paper did not compress while held.");
        Capture(target,"press-held");
        Dump(feedback.GetComponentInChildren<PaperCreaseGraphic>(),"crease-diagnostic");
        ExecuteEvents.Execute(button.gameObject,data,ExecuteEvents.pointerExitHandler);
        Require(!data.eligibleForClick,"Dragging outside must cancel the click.");
        ExecuteEvents.Execute(button.gameObject,data,ExecuteEvents.pointerUpHandler);
        until=Time.realtimeSinceStartup+.25f;while(Time.realtimeSinceStartup<until)yield return null;
        Require(feedback.Amount==0 && Vector3.Distance(button.transform.localScale,Vector3.one)<.001f,"Released paper must recover its shape.");
        data.position=RectTransformUtility.WorldToScreenPoint(Camera.main,rect.TransformPoint(rect.rect.center));data.eligibleForClick=true;
        ExecuteEvents.Execute(button.gameObject,data,ExecuteEvents.pointerEnterHandler);ExecuteEvents.Execute(button.gameObject,data,ExecuteEvents.pointerDownHandler);
        Require(Vector2.Distance(feedback.ContactPoint,rect.rect.center)<.1f,"Center press was misplaced.");
        yield return null;ExecuteEvents.Execute(button.gameObject,data,ExecuteEvents.pointerUpHandler);
        until=Time.realtimeSinceStartup+.25f;while(Time.realtimeSinceStartup<until)yield return null;
        button.interactable=false;ExecuteEvents.Execute(button.gameObject,data,ExecuteEvents.pointerDownHandler);yield return null;
        Require(feedback.Amount==0,"Disabled button must not wrinkle.");button.interactable=true;
        EventSystem.current.SetSelectedGameObject(null);
        var service=PaperPageTransition.Instance;int changes=0;
        Require(PaperPageTransition.ShowPanel(()=>changes++),"First navigation should be accepted.");
        Require(!button.IsInteractable(),"Navigation must block keyboard/controller submission as well as touch.");
        Require(!PaperPageTransition.ShowPanel(()=>changes++),"Repeated navigation should be rejected.");
        yield return null;service.Complete();yield return null;
        Require(changes==1 && !PaperPageTransition.IsTransitioning,"Interrupted transition must commit once and unlock.");
        lobby.OpenProfile();yield return null;
        lobby.OnJoinedLobby(); // Synthetic authoritative callback; never opens a network connection.
        while(PaperPageTransition.IsTransitioning)yield return null;
        Require(lobby.modeSelectionPanel.activeSelf && !lobby.profilePanel.activeSelf,"Latest network panel state must replace a stale destination.");
        lobby.OpenProfile();yield return null;service.SendMessage("OnApplicationPause",true);yield return null;
        Require(!PaperPageTransition.IsTransitioning && lobby.profilePanel.activeSelf,"Pause must finish the valid destination and release input.");
        lobby.nameInput.text="Kalem123456789";lobby.SaveAndCloseProfile();
        while(PaperPageTransition.IsTransitioning)yield return null;
        Require(lobby.currentNameText.text=="Kalem12345","Profile name rules changed.");
        lobby.OpenProfile();yield return null;var resize=new RenderTexture(600,1000,24);Camera.main.targetTexture=resize;
        yield return null;yield return null;
        Require(!PaperPageTransition.IsTransitioning && lobby.profilePanel.activeSelf,"Viewport resize must finish the valid panel and unlock.");
        Bind(target);resize.Release();UnityEngine.Object.Destroy(resize);
        lobby.SaveAndCloseProfile();while(PaperPageTransition.IsTransitioning)yield return null;
        lobby.SelectSingleplayerMode();while(PaperPageTransition.IsTransitioning)yield return null;
        service.ValidationLoadingReady=()=>false;
        var category=lobby.categoryPanel.GetComponentsInChildren<Button>().First(b=>b.GetComponentInChildren<TMP_Text>()?.text=="Genel Kültür");
        category.onClick.Invoke();category.onClick.Invoke();
        Require(GameManager.Instance.CategoryIndex==3 && PaperPageTransition.IsTransitioning,"Category must be selected once before loading.");
        while(service.Phase!="Loading")yield return null;
        until=Time.realtimeSinceStartup+.35f;while(Time.realtimeSinceStartup<until)yield return null;
        var note=service.GetComponentsInChildren<TMP_Text>().First(t=>t.text=="Hazırlanıyor…");
        Require(note.enabled && SceneManager.GetActiveScene().name=="SampleScene","Slow load must hold the clean paper with its loading note.");
        Capture(target,"slow-load-paper");service.ValidationLoadingReady=()=>true;
        Directory.CreateDirectory(Output+"/game-transition-frames");int frame=0;
        while(PaperPageTransition.IsTransitioning){Capture(target,"game-transition-frames/frame-"+(frame++).ToString("D3"));yield return null;}
        service.ValidationLoadingReady=null;
        Require(SceneManager.GetActiveScene().name=="GAMESCENE" && !string.IsNullOrEmpty(GameManager.Instance.CurrentWord),"Real asynchronous game loading failed.");
        Require(GameManager.Instance.WrongGuessCount==0 && !UnityEngine.Object.FindFirstObjectByType<HangmanDrawer>().IsDrawing,"Page entry must not draw a wrong-answer body part.");
        var keyboard=UnityEngine.Object.FindFirstObjectByType<KeyboardUI>();
        Require(keyboard.GetComponentsInChildren<Button>().All(b=>b.GetComponent<PaperPressFeedback>()!=null),"Game keys need paper press feedback.");
        Capture(target,"game-entry");
        char correct=GameManager.Instance.CurrentWord.First(char.IsLetter);
        var key=keyboard.GetComponentsInChildren<Button>().First(b=>b.GetComponentInChildren<TMP_Text>().text==correct.ToString());
        key.onClick.Invoke();until=Time.realtimeSinceStartup+.3f;while(Time.realtimeSinceStartup<until){Require(key.transform.localScale.x<=1.001f,"Old keyboard punch animation remains.");yield return null;}
        Require(!key.interactable && !PaperPageTransition.IsTransitioning,"Letter press must update its state without changing pages.");
        GameManager.Instance.GoToMainMenu();while(PaperPageTransition.IsTransitioning){Bind(target);yield return null;}
        Require(SceneManager.GetActiveScene().name=="SampleScene" && !UnityEngine.Object.FindFirstObjectByType<PaperMenuIntro>().IsDrawing,"Returning to menu must not replay the long opening.");
        bool networkCalled=false;
        PaperPageTransition.CoverNetworkLoad(()=>{networkCalled=true;SceneManager.LoadSceneAsync("MultiplayerGameScene");});
        Require(networkCalled,"Network load dispatch must not wait for erasing.");
        while(PaperPageTransition.IsTransitioning){Bind(target);yield return null;}
        Require(SceneManager.GetActiveScene().name=="MultiplayerGameScene","External scene handoff failed.");
        Require(UnityEngine.Object.FindFirstObjectByType<KeyboardUI>().GetComponentsInChildren<Button>().All(b=>b.GetComponent<PaperPressFeedback>()!=null),"Multiplayer keys need paper feedback.");
        Capture(target,"multiplayer-entry");
        UnityEngine.Object.FindFirstObjectByType<MultiplayerGameManager>().LeaveToMenu();
        while(PaperPageTransition.IsTransitioning){Bind(target);yield return null;}
        Require(SceneManager.GetActiveScene().name=="SampleScene","Multiplayer menu return failed.");
        var wide=new RenderTexture(1080,1920,24);Bind(wide);
        lobby=UnityEngine.Object.FindFirstObjectByType<LobbyManager>();lobby.SelectSingleplayerMode();
        Directory.CreateDirectory(Output+"/transition-9x16-frames");frame=0;
        while(PaperPageTransition.IsTransitioning){Capture(wide,"transition-9x16-frames/frame-"+(frame++).ToString("D3"));yield return null;}
        Capture(wide,"categories-9x16");lobby.BackToModeSelection();while(PaperPageTransition.IsTransitioning)yield return null;
        Bind(target);wide.Release();UnityEngine.Object.Destroy(wide);
        Require(!service.GetComponentsInChildren<Canvas>().Any(c=>c.enabled),"Transition overlay must be disabled after navigation.");
        File.WriteAllText(Output+"/interaction-results.txt","PASS: exact 4.5s configured opening / .55s outline + .35s text; finger-position creases, held compression, release and outside cancellation, disabled keys; repeated navigation rejected; cancelled/pause transition commits once and unlocks; synthetic authoritative network callback wins; profile name limit; held real asynchronous loading shows clean paper/loading note; actual category 3 -> GAMESCENE -> menu; no wrong-answer body drawn by entry; game keyboard has feedback and no punch/page transition on letter; immediate external scene dispatch -> MultiplayerGameScene -> menu; long intro not replayed; 720x1560 and 1080x1920 rendered. PlayerPrefs identity is isolated. Live two-client networking and physical mobile touches/performance not tested.\n");
    }
    static void Bind(RenderTexture target){if(Camera.main!=null)Camera.main.targetTexture=target;}
    static void Capture(RenderTexture target,string name)
    {
        Bind(target);Canvas.ForceUpdateCanvases();var camera=Camera.main;camera.Render();var previous=RenderTexture.active;RenderTexture.active=target;
        var texture=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,target.width,target.height),0,0);texture.Apply();File.WriteAllBytes(Output+"/"+name+".png",texture.EncodeToPNG());UnityEngine.Object.Destroy(texture);RenderTexture.active=previous;
    }
    static void Require(bool value,string message){if(!value)throw new Exception(message);}
    public static void Dump(Graphic graphic,string name)
    {
        var mesh=graphic.canvasRenderer.GetMesh();
        Require(mesh!=null && mesh.vertexCount>0,"Paper effect must have a rendered UI mesh: "+graphic.name);
        File.WriteAllText(Output+"/"+name+".txt",$"enabled={graphic.enabled} active={graphic.gameObject.activeInHierarchy} culled={graphic.canvasRenderer.cull} vertices={mesh.vertexCount} rect={graphic.rectTransform.rect} world={graphic.transform.position} screen={RectTransformUtility.WorldToScreenPoint(Camera.main,graphic.transform.position)} color={graphic.color} rendererColor={graphic.canvasRenderer.GetColor()} canvas={graphic.canvas.name}\n");
    }
}
