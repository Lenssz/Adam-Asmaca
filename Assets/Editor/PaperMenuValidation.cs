using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[InitializeOnLoad]
public static class PaperMenuValidation
{
    const string Key="PaperMenu.Validation";
    static IEnumerator checks;
    static double deadline;
    static PaperMenuValidation()
    {
        EditorApplication.playModeStateChanged+=state=>
        {
            if(!SessionState.GetBool(Key,false))return;
            if(state==PlayModeStateChange.EnteredPlayMode){checks=Check();deadline=EditorApplication.timeSinceStartup+100;EditorApplication.update+=Advance;}
            if(state==PlayModeStateChange.EnteredEditMode){SessionState.SetBool(Key,false);EditorApplication.Exit(File.Exists(PaperMenuBuilder.Output+"/error.txt")?1:0);}
        };
    }
    public static void RunBatch()
    {
        Directory.CreateDirectory(PaperMenuBuilder.Output);
        if(File.Exists(PaperMenuBuilder.Output+"/error.txt"))File.Delete(PaperMenuBuilder.Output+"/error.txt");
        try
        {
            EditorSceneManager.OpenScene(PaperMenuBuilder.ScenePath,OpenSceneMode.Single);
            PaperMenuBuilder.Build();
            PaperInteractionBuilder.Build();
            SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;
        }
        catch(Exception ex){File.WriteAllText(PaperMenuBuilder.Output+"/error.txt",ex.ToString());EditorApplication.Exit(1);}
    }
    static void Advance()
    {
        try{Require(EditorApplication.timeSinceStartup<deadline,"Menu checks timed out.");if(checks.MoveNext())return;}
        catch(Exception ex){File.WriteAllText(PaperMenuBuilder.Output+"/error.txt",ex.ToString());Debug.LogException(ex);}
        EditorApplication.update-=Advance;EditorApplication.isPlaying=false;
    }
    static IEnumerator Check()
    {
        yield return null;
        var lobby=UnityEngine.Object.FindFirstObjectByType<LobbyManager>();
        var canvas=UnityEngine.Object.FindFirstObjectByType<Canvas>();
        var camera=Camera.main;var target=new RenderTexture(720,1560,24);camera.targetTexture=target;
        var intro=canvas.GetComponentInChildren<PaperMenuIntro>();
        Require(intro!=null && intro.IsDrawing,"Opening drawing did not start.");
        Require(Mathf.Abs(intro.duration-4.5f)<.02f,"Opening duration must be approximately 4.5 seconds.");
        foreach(var item in intro.drawingOrder.Where(i=>i.graphic.GetComponent<Button>()!=null))Require(Mathf.Approximately(item.seconds,.55f),"Button outline needs .55 seconds.");
        Require(!intro.GetComponent<CanvasGroup>().interactable,"Menu input should wait for its short opening.");
        Require(intro.strokeMaterial.shader.isSupported && !ShaderUtil.ShaderHasError(intro.strokeMaterial.shader),"Opening shader is unsupported or failed to compile.");
        Directory.CreateDirectory(PaperMenuBuilder.Output+"/intro-frames");
        int frame=0;float next=0;
        while(intro.IsDrawing)
        {
            if(intro.Progress>=next)
            {
                Canvas.ForceUpdateCanvases();Capture(camera,target,"intro-frames/frame-"+frame.ToString("D3"));frame++;next+=1f/32;
            }
            yield return null;
        }
        Require(intro.Progress==1 && !intro.PencilVisible && intro.GetComponent<CanvasGroup>().interactable,"Opening did not release input or hide its pencil.");
        Require(intro.drawingOrder.All(item=>item.graphic.enabled),"Opening left a menu graphic hidden.");
        Require(!intro.GetComponentsInChildren<RectMask2D>().Any(),"Opening left a temporary text mask.");
        float settle=Time.realtimeSinceStartup+.25f;while(Time.realtimeSinceStartup<settle)yield return null;
        Canvas.ForceUpdateCanvases();Capture(camera,target,"intro-frames/frame-"+frame.ToString("D3"));
        Capture(camera,target,"intro-complete");
        intro.Replay();yield return null;intro.enabled=false;yield return null;
        settle=Time.realtimeSinceStartup+.25f;while(Time.realtimeSinceStartup<settle)yield return null;
        Canvas.ForceUpdateCanvases();Capture(camera,target,"intro-cancelled");
        Require(!intro.IsDrawing && !intro.PencilVisible && intro.GetComponent<CanvasGroup>().interactable,"Interrupted opening did not clean up.");
        Require(File.ReadAllBytes(PaperMenuBuilder.Output+"/intro-complete.png").SequenceEqual(File.ReadAllBytes(PaperMenuBuilder.Output+"/intro-cancelled.png")),"Interrupted opening changed the completed artwork/layout.");
        intro.enabled=true;yield return null;Require(!intro.IsDrawing,"Opening replayed on re-enable.");
        Require(lobby.modeSelectionPanel.activeSelf && !lobby.categoryPanel.activeSelf && !lobby.profilePanel.activeSelf,"Initial menu panel incorrect.");
        var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Paper/PatrickHand SDF.asset");
        foreach(var text in canvas.GetComponentsInChildren<TMP_Text>(true))Require(text.font==font,"Menu text has a different font: "+text.name);
        Require(!canvas.transform.Find("Menu notebook paper").GetComponent<Image>().raycastTarget,"Paper blocks clicks.");
        var outline=new Texture2D(2,2);outline.LoadImage(File.ReadAllBytes("Assets/Sprites/PaperTheme/pencil-outline.png"));
        Require(outline.GetPixel(outline.width/2,outline.height/2).a==0,"Menu frame interior must be transparent.");UnityEngine.Object.Destroy(outline);
        var logos=lobby.categoryPanel.GetComponentsInChildren<Image>(true).Where(i=>i.name=="Pencil category logo").ToArray();
        Require(logos.Length==6,"All six pencil logos must be present.");
        foreach(var logo in logos)
        {
            Require(logo.sprite!=null && logo.preserveAspect && !logo.raycastTarget,"Category logo must preserve its shape and allow clicks through.");
            var button=logo.GetComponentInParent<Button>(true);var rect=button.GetComponent<RectTransform>();
            Require(Mathf.Approximately(rect.sizeDelta.x,rect.sizeDelta.y),"Category frame must be square.");
            Require(button.GetComponent<Image>().sprite.name=="pencil-outline","Category must use the transparent pencil outline.");
        }
        yield return null;Canvas.ForceUpdateCanvases();Capture(camera,target,"main-menu");
        var single=lobby.modeSelectionPanel.GetComponentsInChildren<Button>().First(b=>b.GetComponentInChildren<TMP_Text>().text=="Tek Oyunculu");
        Require(single.onClick.GetPersistentMethodName(0)=="SelectSingleplayerMode","Single player action changed.");
        single.onClick.Invoke();single.onClick.Invoke();
        Require(PaperPageTransition.IsTransitioning && lobby.modeSelectionPanel.activeSelf,"Navigation should erase the old panel before replacing it.");
        Directory.CreateDirectory(PaperMenuBuilder.Output+"/transition-frames");int transitionFrame=0;
        while(PaperPageTransition.IsTransitioning){Canvas.ForceUpdateCanvases();Capture(camera,target,"transition-frames/frame-"+(transitionFrame++).ToString("D3"));if(PaperPageTransition.Instance.Phase=="Erasing")PaperInteractionValidation.Dump(PaperPageTransition.Instance.GetComponentInChildren<PaperEraserGraphic>(),"eraser-diagnostic");yield return null;}
        Require(lobby.categoryPanel.activeSelf && !lobby.modeSelectionPanel.activeSelf,"Single player navigation failed.");
        yield return null;Canvas.ForceUpdateCanvases();Capture(camera,target,"categories");
        var back=lobby.categoryPanel.transform.Find("Category back").GetComponent<Button>();back.onClick.Invoke();
        while(PaperPageTransition.IsTransitioning)yield return null;
        Require(lobby.modeSelectionPanel.activeSelf && !lobby.categoryPanel.activeSelf,"Category back action failed.");
        var profile=lobby.modeSelectionPanel.GetComponentsInChildren<Button>().First(b=>b.GetComponentInChildren<TMP_Text>().text=="Profilim");profile.onClick.Invoke();
        while(PaperPageTransition.IsTransitioning)yield return null;
        Require(lobby.profilePanel.activeSelf && !lobby.modeSelectionPanel.activeSelf,"Profile navigation failed.");
        Require(lobby.nameInput.fontAsset==font && lobby.nameInput.interactable,"Profile input is not usable.");
        // Local preview text only: never save or change the player's nickname.
        lobby.currentNameText.text="Çizgi";lobby.nameInput.text="Çizgi";
        yield return null;Canvas.ForceUpdateCanvases();Capture(camera,target,"profile");
        lobby.profilePanel.SetActive(false);lobby.modeSelectionPanel.SetActive(true);
        camera.targetTexture=null;target.Release();UnityEngine.Object.Destroy(target);
        var wide=new RenderTexture(1080,1920,24);camera.targetTexture=wide;yield return null;Canvas.ForceUpdateCanvases();Capture(camera,wide,"main-menu-9x16");camera.targetTexture=null;wide.Release();UnityEngine.Object.Destroy(wide);
        var interactionTarget=new RenderTexture(720,1560,24);camera.targetTexture=interactionTarget;
        var interactionChecks=PaperInteractionValidation.Check(interactionTarget);
        while(interactionChecks.MoveNext())yield return interactionChecks.Current;
        if(Camera.main!=null)Camera.main.targetTexture=null;interactionTarget.Release();UnityEngine.Object.Destroy(interactionTarget);
        File.WriteAllText(PaperMenuBuilder.Output+"/results.txt","PASS: 4.5s configured opening drawing starts, UI shader supported/compiles, sequential frames captured, input restored and pencil/masks removed at completion; interrupted opening restores identical rendered final artwork; re-enable does not replay. Main menu and category/profile panel previews at 720x1560 and 1080x1920; six pencil logos in square outline frames; transparent frame interiors; logos preserve aspect and do not block clicks; Patrick Hand on all menu texts; background does not block clicks; existing persistent button bindings preserved; actual category/back/profile navigation. Extended press, cancellation, slow async loading, scene handoff and viewport-resize results: interaction-results.txt. Test PlayerPrefs identity is isolated; no live Photon connection was made. Mobile hardware not tested.\n");
    }
    static void Capture(Camera camera,RenderTexture target,string name)
    {
        var previous=RenderTexture.active;camera.Render();RenderTexture.active=target;
        var texture=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,target.width,target.height),0,0);texture.Apply();File.WriteAllBytes(PaperMenuBuilder.Output+"/"+name+".png",texture.EncodeToPNG());UnityEngine.Object.Destroy(texture);RenderTexture.active=previous;
    }
    static void Require(bool value,string message){if(!value)throw new Exception(message);}
}
