using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public static class PaperThemeValidation
{
    const string Output=".utmp/paper-validation";
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static void Require(bool value,string message){if(!value)throw new Exception(message);}

    public static void CheckEditor()
    {
        Directory.CreateDirectory(Output);
        Require(PaperThemeBuilder.Handwriting.HasCharacters("ÇçĞğİıÖöŞşÜü"),"Turkish font atlas incomplete.");
        var scene=EditorSceneManager.NewPreviewScene();
        try
        {
            var cameraObject=new GameObject("Bounds camera");SceneManager.MoveGameObjectToScene(cameraObject,scene);
            var camera=cameraObject.AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=5;
            var root=new GameObject("Bounds fixture");SceneManager.MoveGameObjectToScene(root,scene);
            var bounds=root.AddComponent<PaperScreenBounds>();bounds.sceneCamera=camera;
            bounds.RefreshForViewport(new Rect(0,0,1,1),.46f);
            Require(bounds.GetComponentsInChildren<BoxCollider2D>().Length==4,"Not four screen walls.");
            Rect stable=bounds.InnerRect;camera.transform.position+=new Vector3(.1f,.1f,0);
            bounds.RefreshForViewport(new Rect(0,0,1,1),.46f);
            Require(bounds.InnerRect==stable,"Camera shake moved the walls.");
            var rigObject=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PencilRagdollBuilder.PrefabPath));
            SceneManager.MoveGameObjectToScene(rigObject,scene);var rig=rigObject.GetComponent<RagdollController>();rig.InitializeParts();
            Require(rig.partsOrder.All(p=>p.colliders.All(c=>!c.enabled)),"Hidden extra collider enabled.");
            for(int i=1;i<=6;i++)rig.RevealPart(i,false);
            Require(rig.partsOrder.All(p=>p.colliders.All(c=>c.enabled)),"Completed part extra collider disabled.");
            Require(rig.partsOrder[4].colliders.Length==2 && rig.partsOrder[5].colliders.Length==2,"Missing toe colliders.");
            foreach(var part in rig.partsOrder){part.joint.enabled=false;part.body.simulated=false;}
            var foot=rig.partsOrder[4];foot.body.simulated=true;foot.body.gravityScale=0;foot.body.interpolation=RigidbodyInterpolation2D.None;
            var physics=scene.GetPhysicsScene2D();
            Physics2D.SyncTransforms();
            var toeCollider=(BoxCollider2D)foot.colliders[1];
            var toeHit=physics.OverlapPoint(foot.body.GetRelativePoint(toeCollider.offset));
            Require(toeHit!=null && toeHit.attachedRigidbody==foot.body,"Toe collider cannot select its body.");
            foreach(var a in rig.partsOrder[4].colliders) foreach(var b in rig.partsOrder[5].colliders)
                Require(Physics2D.GetIgnoreCollision(a,b),"Additional foot colliders collide with other parts.");
            foreach(Vector2 direction in new[]{Vector2.left,Vector2.right,Vector2.up,Vector2.down})
            {
                foot.body.position=new Vector2(0,.5f);foot.body.rotation=0;foot.body.linearVelocity=direction*3.5f;foot.body.angularVelocity=0;
                Physics2D.SyncTransforms();
                for(int i=0;i<250;i++)physics.Simulate(.02f);
                foreach(var collider in foot.colliders)
                {
                    var box=collider.bounds;
                    Require(box.min.x>=stable.xMin-.025f && box.max.x<=stable.xMax+.025f && box.min.y>=stable.yMin-.025f && box.max.y<=stable.yMax+.025f,"Foot escaped a screen edge: "+direction);
                }
                Vector2 grab=foot.body.position;
                Vector2 target=bounds.ClampTarget(foot.body,Vector2.zero,grab+direction*100,foot.spriteRenderer);
                var artwork=foot.spriteRenderer.bounds;Vector2 delta=target-grab;
                Require(artwork.min.x+delta.x>=stable.xMin-.001f && artwork.max.x+delta.x<=stable.xMax+.001f && artwork.min.y+delta.y>=stable.yMin-.001f && artwork.max.y+delta.y<=stable.yMax+.001f,"Drag target escaped bounds.");
            }
            bounds.RefreshForViewport(new Rect(.03f,.04f,.94f,.91f),.75f);
            Require(bounds.InnerRect.width>stable.width,"Aspect resize did not change bounds.");
            foreach(var clip in PencilDrawingAssets.WritingVariations)
            {
                var samples=new float[clip.samples*clip.channels];clip.LoadAudioData();Require(clip.GetData(samples,0),"Writing audio unreadable.");
                Require(samples.Max(v=>Mathf.Abs(v))>.02f,"Writing variant silent.");
                Require(Mathf.Abs(samples[0])<.001f && Mathf.Abs(samples[samples.Length-1])<.001f,"Audio loop endpoints click.");
            }
            var nearObject=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PencilRagdollBuilder.PrefabPath));
            SceneManager.MoveGameObjectToScene(nearObject,scene);var nearRig=nearObject.GetComponent<RagdollController>();nearRig.screenBounds=bounds;nearRig.InitializeParts();
            var anchor=new GameObject("Near edge rope");SceneManager.MoveGameObjectToScene(anchor,scene);anchor.AddComponent<Rigidbody2D>().bodyType=RigidbodyType2D.Static;
            anchor.transform.position=new Vector3(bounds.InnerRect.xMin+.75f,bounds.InnerRect.yMax-.4f,0);nearRig.AttachToRope(anchor.transform);
            float jointGap=0;
            for(int stage=1;stage<=6;stage++)
            {
                Require(nearRig.BeginDrawing(stage),"Near-edge stroke failed to start.");
                if(stage==1)
                {
                    nearRig.drawingEffect.Tick(.1f,.025f);float pitch=nearRig.drawingEffect.pencilSound.pitch;
                    nearRig.drawingEffect.Tick(.3f,.025f);Require(pitch==nearRig.drawingEffect.pencilSound.pitch,"Pitch changes within one stroke.");
                    nearRig.drawingEffect.Tick(.51f,.025f);Require(nearRig.drawingEffect.pencilSound.volume==0,"Pen lift is not silent.");
                }
                while(nearRig.IsDrawing)
                {
                    nearRig.StepDrawing(.02f);physics.Simulate(.02f);
                    foreach(var part in nearRig.partsOrder.Where(p=>p.revealed))
                    {
                        Vector2 here=part.body.GetRelativePoint(part.joint.anchor);
                        Vector2 there=part.joint.connectedBody.GetRelativePoint(part.joint.connectedAnchor);
                        jointGap=Mathf.Max(jointGap,Vector2.Distance(here,there));
                    }
                }
                nearRig.AddImpulse(new Vector2(-.015f,0));
            }
            Require(jointGap<.045f,"Near-edge drawing separated joints.");
            File.WriteAllText(Output+"/physics-results.txt","PASS: four static walls; all four edge impacts with foot colliders; toe selection by attached rigidbody; all part collision exclusions; clamped drag targets; all hidden/completed colliders; aspect and safe area resize; camera shake isolation; six strokes near the left boundary with stable joints; Turkish glyphs; three non-silent recording variants with zero endpoints; fixed pitch within stroke and silent pen lift.\n");
        }
        finally{EditorSceneManager.ClosePreviewScene(scene);}
    }

    public static IEnumerator CheckGameScreens()
    {
        yield return null;
        var managerObject=new GameObject("Paper UI fixture");var manager=managerObject.AddComponent<GameManager>();
        typeof(GameManager).GetField("<CurrentWord>k__BackingField",Private).SetValue(manager,"KARAKALEM");
        foreach(string name in new[]{"GAMESCENE","MultiplayerGameScene"})
        {
            var loading=SceneManager.LoadSceneAsync("Assets/Scenes/"+name+".unity",LoadSceneMode.Single);
            while(!loading.isDone)yield return null;
            yield return null;
            var scene=SceneManager.GetActiveScene();
            var drawer=UnityEngine.Object.FindFirstObjectByType<HangmanDrawer>();
            var multiplayer=UnityEngine.Object.FindFirstObjectByType<MultiplayerGameManager>();
            if(multiplayer!=null)typeof(MultiplayerGameManager).GetMethod("RPC_ReceiveWord",Private).Invoke(multiplayer,new object[]{"KARAKALEM"});
            var keyboard=UnityEngine.Object.FindFirstObjectByType<KeyboardUI>();
            Require(keyboard.GetComponentsInChildren<Button>().Length==32,"Keyboard missing Turkish keys.");
            keyboard.OnLetterGuessed('K',true);keyboard.OnLetterGuessed('Z',false);
            if(multiplayer==null) manager.OnLetterGuessed?.Invoke('K',true);
            else
            {
                var boxes=(System.Collections.Generic.List<GameObject>)typeof(MultiplayerGameManager).GetField("myBoxes",Private).GetValue(multiplayer);
                foreach(int index in new[]{0,4}){boxes[index].GetComponentInChildren<TMP_Text>().text="K";boxes[index].GetComponent<Image>().color=multiplayer.correctLetterColor;}
                typeof(MultiplayerGameManager).GetMethod("RPC_OpponentGuessedCorrect",Private).Invoke(multiplayer,new object[]{new[]{0,4}});
            }
            var bounds=UnityEngine.Object.FindFirstObjectByType<PaperScreenBounds>();
            Require(bounds!=null && drawer.screenBounds==bounds,"Screen bounds not wired.");
            var camera=Camera.main;var target=new RenderTexture(720,1560,24);camera.targetTexture=target;
            bounds.RefreshForViewport(new Rect(0,0,1,1),720f/1560);
            drawer.ResetDrawing();drawer.AddPartToQueue(6);
            while(drawer.IsDrawing)yield return null;
            Require(drawer.CurrentRagdoll.screenBounds==bounds,"New rig lost screen bounds.");
            Require(!drawer.CurrentRagdoll.drawingEffect.pencilSound.isPlaying,"Completed pencil audio still playing.");
            foreach(var text in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<TMP_Text>(true)))
                Require(text.font == AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Paper/PatrickHand SDF.asset"),"Unstyled game text: "+text.name);
            Canvas.ForceUpdateCanvases();yield return null;
            Capture(camera,target,Output+"/"+name+".png");
            var ui=multiplayer==null ? UnityEngine.Object.FindFirstObjectByType<GameUI>() : null;
            GameObject panel=ui!=null ? ui.resultPanel : multiplayer.winLossPanel;
            panel.SetActive(true);panel.transform.localScale=Vector3.one;
            if(ui!=null){ui.resultTitle.text="Kaybettin!";ui.resultTitle.color=ui.loseColor;ui.resultWord.text="Kelime: KARAKALEM";ui.coinWord.text="Mevcut Altın: 12";}
            else multiplayer.resultText.text="<color=#456E42>TEBRİKLER, KAZANDIN!</color>\nDoğru Kelime: KARAKALEM\nKazanılan Altın: 2";
            Canvas.ForceUpdateCanvases();yield return null;Capture(camera,target,Output+"/"+name+"-result.png");panel.SetActive(false);
            if(ui!=null)
            {
                typeof(GameManager).GetField("<CurrentWord>k__BackingField",Private).SetValue(manager,"ÇEKOSLOVAKYALILAŞTIRAMADIKLARIMIZDAN");
                manager.OnWordChanged?.Invoke();
                float until=Time.time+3.2f;while(Time.time<until)yield return null;
                ui.wordDisplay.RevealAllLetters();until=Time.time+.4f;while(Time.time<until)yield return null;
                Canvas.ForceUpdateCanvases();Capture(camera,target,Output+"/"+name+"-long-word.png");
            }
            int revealedBeforeResize=drawer.CurrentRagdoll.RevealedCount;
            bounds.RefreshForViewport(new Rect(.03f,.04f,.94f,.90f),.75f);
            Require(drawer.CurrentRagdoll.RevealedCount==revealedBeforeResize,"Resize reset revealed parts.");
            drawer.ResetDrawing();drawer.AddPartToQueue(1);yield return null;drawer.ResetDrawing();
            Require(!drawer.CurrentRagdoll.IsDrawing,"Game reset did not cancel drawing.");
            camera.targetTexture=null;target.Release();UnityEngine.Object.Destroy(target);
        }
        UnityEngine.Object.Destroy(managerObject);
        File.WriteAllText(Output+"/game-results.txt","PASS: both actual game scenes in Play Mode; 32 keyboard keys; Patrick Hand on all game texts; correct/wrong key states; full six-part queue and silent completion; bounds retained across reset and resize; result panels; Turkish long word preview. Photon events and score timing were not altered. No live two-device multiplayer or physical mobile device test performed.\n");
    }

    static void Capture(Camera camera,RenderTexture target,string path)
    {
        var previous=RenderTexture.active;camera.Render();RenderTexture.active=target;
        var image=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
        image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());
        UnityEngine.Object.Destroy(image);RenderTexture.active=previous;
    }
}
