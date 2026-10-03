using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class PencilDrawingValidation
{
    const string Output = ".utmp/pencil-validation";
    const string BatchKey = "PencilDrawing.BatchValidation";
    static IEnumerator playChecks;
    static double playDeadline;

    static PencilDrawingValidation()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (!SessionState.GetBool(BatchKey, false)) return;
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                playChecks = CheckPlayMode();
                playDeadline = EditorApplication.timeSinceStartup + 90;
                EditorApplication.update += AdvancePlayChecks;
            }
            else if (state == PlayModeStateChange.EnteredEditMode)
            {
                SessionState.SetBool(BatchKey, false);
                if (Application.isBatchMode) EditorApplication.Exit(File.Exists(Output + "/drawing-play-error.txt") ? 1 : 0);
            }
        };
    }

    // Run in an isolated project with Unity -batchmode -executeMethod PencilDrawingValidation.RunBatch (without -quit).
    public static void RunBatch()
    {
        try
        {
            Directory.CreateDirectory(Output);
            foreach (string name in new[] { "drawing-error.txt", "drawing-play-error.txt", "drawing-results.txt", "drawing-play-results.txt" })
                if (File.Exists(Output + "/" + name)) File.Delete(Output + "/" + name);
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
            PencilRagdollBuilder.BuildAndValidate();
            Require(!File.Exists(Output + "/build-error.txt"), "Prefab build failed; see build-error.txt.");
            ValidateDrawing();
            PencilRagdollBuilder.ApplyToScenes();
            PaperThemeValidation.CheckEditor();
            EditorSceneManager.OpenScene(PencilRagdollBuilder.PreviewPath, OpenSceneMode.Single);
            SessionState.SetBool(BatchKey, true);
            EditorApplication.isPlaying = true;
        }
        catch (Exception ex)
        {
            File.WriteAllText(Output + "/drawing-error.txt", ex.ToString());
            Debug.LogException(ex);
            if (Application.isBatchMode) EditorApplication.Exit(1);
        }
    }

    [MenuItem("Tools/Pencil Stickman/4 Validate pencil drawing")]
    public static void ValidateDrawing()
    {
        Directory.CreateDirectory(Output);
        var scene = EditorSceneManager.NewPreviewScene();
        try
        {
            var anchor = new GameObject("Drawing test rope"); SceneManager.MoveGameObjectToScene(anchor, scene);
            anchor.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
            var model = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PencilRagdollBuilder.PrefabPath));
            SceneManager.MoveGameObjectToScene(model, scene);
            foreach (Transform child in model.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 31;
            var rig = model.GetComponent<RagdollController>(); rig.InitializeParts(); rig.AttachToRope(anchor.transform);
            var physics = scene.GetPhysicsScene2D();
            float maxPreviewGap = 0;
            for (int index = 0; index < 6; index++)
            {
                var part = rig.partsOrder[index];
                Require(part.strokeProfile != null && part.strokeProfile.orderMask != null, "Missing stroke assets.");
                Require(Mathf.Abs(part.strokeProfile.duration - (index == 0 ? .9f : .5f)) < .001f, "Wrong duration.");
                Require(rig.BeginDrawing(index + 1), "Drawing did not start.");
                Require(!rig.BeginDrawing(index + 1), "Duplicate drawing was accepted.");
                for (int quarter = 0; quarter < 4; quarter++)
                {
                    Require(!part.body.simulated && !part.partCollider.enabled && !part.joint.enabled, "Unfinished part affects physics.");
                    Require(rig.RevealedCount == index, "An unfinished part was counted as revealed.");
                    var block = new MaterialPropertyBlock(); part.spriteRenderer.GetPropertyBlock(block);
                    Require(Mathf.Abs(block.GetFloat("_DrawProgress") - quarter / 4f) < .001f, "Wrong shader progress.");
                    Vector2 uv = part.strokeProfile.Evaluate(block.GetFloat("_DrawProgress"), out bool down, out float lift);
                    Vector2 local = (Vector2.Scale(uv, part.spriteRenderer.sprite.rect.size) - part.spriteRenderer.sprite.pivot) / part.spriteRenderer.sprite.pixelsPerUnit;
                    Vector3 expected = part.spriteRenderer.transform.TransformPoint(local) + new Vector3(0,lift,0);
                    Require(Vector3.Distance(expected, rig.drawingEffect.pencilTip.transform.position) < .0001f, "Pencil tip does not follow the stroke.");
                    PencilRagdollBuilder.Render(model, scene, Output + "/drawing-" + (index + 1) + "-" + quarter + ".png");
                    for (int step = 0; step < 10; step++)
                    {
                        physics.Simulate(part.strokeProfile.duration / 40);
                        rig.StepDrawing(part.strokeProfile.duration / 40);
                        if (rig.IsDrawing)
                        {
                            Vector2 parentAnchor = part.joint.connectedBody.GetRelativePoint(part.joint.connectedAnchor);
                            maxPreviewGap = Mathf.Max(maxPreviewGap, Vector2.Distance(parentAnchor, part.body.GetRelativePoint(part.joint.anchor)));
                        }
                    }
                }
                if (rig.IsDrawing) rig.StepDrawing(.00001f);
                Require(!rig.IsDrawing && rig.RevealedCount == index + 1 && part.body.simulated && part.partCollider.enabled, "Physics handoff failed.");
                Require(!rig.drawingEffect.pencilTip.enabled && rig.drawingEffect.pencilSound.volume == 0, "Pencil or sound remained active.");
                PencilRagdollBuilder.Render(model, scene, Output + "/drawing-" + (index + 1) + "-4.png");
                if (index == 1) rig.AddImpulse(new Vector2(.07f,0));
            }
            Require(maxPreviewGap < .002f, "Unfinished part detached from moving parent.");
            PencilRagdollBuilder.Render(model, scene, Output + "/drawing-complete.png");
            var originalMaterial = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat");
            foreach (var part in rig.partsOrder) part.spriteRenderer.sharedMaterial = originalMaterial;
            PencilRagdollBuilder.Render(model, scene, Output + "/drawing-original-reference.png");
            var actualImage = new Texture2D(2,2); actualImage.LoadImage(File.ReadAllBytes(Output + "/drawing-complete.png"));
            var referenceImage = new Texture2D(2,2); referenceImage.LoadImage(File.ReadAllBytes(Output + "/drawing-original-reference.png"));
            var actualPixels = actualImage.GetPixels32(); var referencePixels = referenceImage.GetPixels32();
            int maxDifference = 0;
            for (int i = 0; i < actualPixels.Length; i++)
                maxDifference = Mathf.Max(maxDifference, Mathf.Abs(actualPixels[i].r - referencePixels[i].r), Mathf.Abs(actualPixels[i].g - referencePixels[i].g), Mathf.Abs(actualPixels[i].b - referencePixels[i].b));
            UnityEngine.Object.DestroyImmediate(actualImage); UnityEngine.Object.DestroyImmediate(referenceImage);
            Require(maxDifference <= 1, "Completed drawing differs from original sprite rendering: " + maxDifference);
            // Cancellation must allow a silent complete reveal of the same renderer.
            var resetModel = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PencilRagdollBuilder.PrefabPath));
            SceneManager.MoveGameObjectToScene(resetModel, scene);
            var resetRig = resetModel.GetComponent<RagdollController>(); resetRig.InitializeParts(); resetRig.AttachToRope(anchor.transform);
            resetRig.BeginDrawing(1); resetRig.StepDrawing(.2f); resetRig.CancelDrawing(); resetModel.SetActive(false); resetModel.SetActive(true);
            resetRig.RevealPart(1, false);
            var completed = new MaterialPropertyBlock(); resetRig.partsOrder[0].spriteRenderer.GetPropertyBlock(completed);
            Require(completed.GetFloat("_DrawProgress") == 1 && !resetRig.IsDrawing, "Silent reveal after cancellation is incomplete.");
            File.WriteAllText(Output + "/drawing-results.txt", "PASS: all six stroke profiles; quarter-progress renders; tip/path alignment; duplicate prevention; disabled drawing physics; moving-parent following; completion handoff; original completed appearance; cancellation and silent reveal.\nMax drawing anchor gap: " + maxPreviewGap.ToString("F7") + " units.\nMax completed RGB difference: " + maxDifference + "/255.");
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }

    static IEnumerator CheckPlayMode()
    {
        var preview = UnityEngine.Object.FindFirstObjectByType<PencilRagdollPreview>();
        Require(preview != null, "Workshop is missing.");
        yield return null;
        preview.enabled = false;
        var drawer = preview.drawer;
        drawer.ResetDrawing();
        var cancelled = drawer.CurrentRagdoll;
        drawer.AddPartToQueue(1);
        double until = EditorApplication.timeSinceStartup + .2;
        while (EditorApplication.timeSinceStartup < until) yield return null;
        Require(cancelled.IsDrawing && cancelled.RevealedCount == 0 && cancelled.drawingEffect.pencilSound.isPlaying, "Head drawing/audio did not start.");
        drawer.ResetDrawing();
        Require(!cancelled.IsDrawing && !cancelled.drawingEffect.pencilTip.enabled && !cancelled.drawingEffect.pencilSound.isPlaying, "Reset did not stop the old effect immediately.");
        Require(!drawer.IsDrawing && drawer.CurrentRagdoll.RevealedCount == 0, "Reset did not clear the queue.");
        drawer.AddPartToQueue(6); drawer.AddPartToQueue(6); drawer.AddPartToQueue(4);
        Vector3 cameraRest = Camera.main.transform.localPosition;
        drawer.PlayLoseEffect();
        var rig = drawer.CurrentRagdoll;
        foreach (Transform child in rig.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 31;
        string animationFolder = Output + "/animation";
        Directory.CreateDirectory(animationFolder);
        var frameTimes = new System.Collections.Generic.List<float>();
        int previous = 0;
        while (drawer.IsDrawing)
        {
            Require(rig.RevealedCount >= previous && rig.RevealedCount <= previous + 1, "Queued drawings overlapped or repeated.");
            previous = rig.RevealedCount;
            if (rig.RevealedCount < 6) Require(Vector3.Distance(Camera.main.transform.localPosition, cameraRest) < .0001f, "Loss shake started before the last drawing finished.");
            if (rig.IsDrawing)
            {
                var active = rig.partsOrder.First(p => p.spriteRenderer.enabled && !p.revealed);
                Require(!active.body.simulated && !active.partCollider.enabled, "Drawing part is draggable.");
                Require(rig.drawingEffect.pencilSound.isPlaying, "Drawing sound is missing.");
            }
            if (frameTimes.Count == 0 || Time.time - frameTimes[frameTimes.Count - 1] >= .045f)
            {
                PencilRagdollBuilder.Render(rig.gameObject, rig.gameObject.scene, animationFolder + "/" + frameTimes.Count.ToString("D4") + ".png");
                frameTimes.Add(Time.time);
            }
            yield return null;
        }
        PencilRagdollBuilder.Render(rig.gameObject, rig.gameObject.scene, animationFolder + "/" + frameTimes.Count.ToString("D4") + ".png");
        frameTimes.Add(Time.time);
        File.WriteAllText(animationFolder + "/times.txt", string.Join("\n", frameTimes.Select(t => t.ToString("F4", System.Globalization.CultureInfo.InvariantCulture))));
        Require(rig.RevealedCount == 6 && !rig.drawingEffect.pencilTip.enabled && !rig.drawingEffect.pencilSound.isPlaying, "Six-part queue did not finish cleanly.");
        until = EditorApplication.timeSinceStartup + .6;
        while (EditorApplication.timeSinceStartup < until) yield return null;
        Require(Vector3.Distance(Camera.main.transform.localPosition, cameraRest) < .0001f, "Loss shake did not restore the camera.");
        drawer.ResetDrawing(); drawer.AddPartToQueue(3);
        while (drawer.CurrentRagdoll.RevealedCount < 2) yield return null;
        while (!drawer.CurrentRagdoll.IsDrawing) yield return null;
        cancelled = drawer.CurrentRagdoll;
        drawer.ResetDrawing();
        Require(!cancelled.IsDrawing && !cancelled.drawingEffect.pencilSound.isPlaying, "Limb cancellation did not stop audio.");
        Require(UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length == 1, "Workshop has the wrong number of listeners.");

        // Exercise the real result coroutine with local UI fixtures, without selecting
        // words, saving a category pool, awarding coins, or connecting to Photon.
        var managerObject = new GameObject("Result test manager");
        var manager = managerObject.AddComponent<GameManager>(); manager.enabled = false;
        typeof(GameManager).GetField("<CurrentWord>k__BackingField", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(manager, "TEST");
        var uiObject = new GameObject("Result timing fixture"); uiObject.SetActive(false);
        var ui = uiObject.AddComponent<GameUI>();
        ui.hangmanDrawer = drawer;
        ui.resultPanel = new GameObject("Result panel", typeof(RectTransform)); ui.resultPanel.transform.SetParent(uiObject.transform);
        ui.resultTitle = Label(uiObject.transform, "Title"); ui.resultWord = Label(uiObject.transform, "Word"); ui.coinWord = Label(uiObject.transform, "Coins");
        ui.playAgainButton = new GameObject("Again", typeof(RectTransform), typeof(UnityEngine.UI.Button)).GetComponent<UnityEngine.UI.Button>(); ui.playAgainButton.transform.SetParent(uiObject.transform);
        ui.backMenuButton = new GameObject("Back", typeof(RectTransform), typeof(UnityEngine.UI.Button)).GetComponent<UnityEngine.UI.Button>(); ui.backMenuButton.transform.SetParent(uiObject.transform);
        var wordObject = new GameObject("Inactive word fixture"); wordObject.SetActive(false);
        ui.wordDisplay = wordObject.AddComponent<WordDisplay>();
        var texts = new System.Collections.Generic.List<TMPro.TMP_Text>(); var images = new System.Collections.Generic.List<UnityEngine.UI.Image>();
        for (int i = 0; i < 4; i++)
        {
            var text = Label(wordObject.transform, "Slot " + i); text.text = ""; texts.Add(text);
            var imageObject = new GameObject("Slot image " + i, typeof(RectTransform), typeof(UnityEngine.UI.Image));
            imageObject.transform.SetParent(wordObject.transform, false);
            images.Add(imageObject.GetComponent<UnityEngine.UI.Image>());
        }
        var fields = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        typeof(WordDisplay).GetField("letterTexts", fields).SetValue(ui.wordDisplay, texts);
        typeof(WordDisplay).GetField("slotImages", fields).SetValue(ui.wordDisplay, images);
        uiObject.SetActive(true); yield return null;
        drawer.ResetDrawing(); drawer.AddPartToQueue(6); manager.OnGameEnd?.Invoke(false);
        while (drawer.IsDrawing)
        {
            Require(!ui.resultPanel.activeSelf, "Result panel covered an unfinished drawing.");
            yield return null;
        }
        until = EditorApplication.timeSinceStartup + .2;
        while (!ui.resultPanel.activeSelf && EditorApplication.timeSinceStartup < until) yield return null;
        Require(ui.resultPanel.activeSelf, "Result panel did not appear after drawing.");
        ui.resultPanel.SetActive(false); drawer.ResetDrawing(); drawer.AddPartToQueue(1); manager.OnGameEnd?.Invoke(false); drawer.ResetDrawing();
        until = EditorApplication.timeSinceStartup + 1;
        while (EditorApplication.timeSinceStartup < until) yield return null;
        Require(!ui.resultPanel.activeSelf, "An old result appeared after a reset.");
        uiObject.SetActive(false);
        UnityEngine.Object.Destroy(uiObject); UnityEngine.Object.Destroy(wordObject); UnityEngine.Object.Destroy(managerObject);
        var paperChecks = PaperThemeValidation.CheckGameScreens();
        while (paperChecks.MoveNext()) yield return paperChecks.Current;
        File.WriteAllText(Output + "/drawing-play-results.txt", "PASS: actual Play Mode coroutines; drawing sound playback; reset during head and limb; immediate audio cancellation; rapid 1-to-6 queue; duplicate/decreasing notifications; serial physics handoff; deferred loss shake and camera restoration; single AudioListener; result panel waits for drawing; stale result cancelled after reset.");
    }

    static void AdvancePlayChecks()
    {
        try
        {
            Require(EditorApplication.timeSinceStartup < playDeadline, "Play Mode validation timed out.");
            if (playChecks.MoveNext()) return;
        }
        catch (Exception ex)
        {
            File.WriteAllText(Output + "/drawing-play-error.txt", ex.ToString());
            Debug.LogException(ex);
        }
        EditorApplication.update -= AdvancePlayChecks;
        playChecks = null;
        EditorApplication.isPlaying = false;
    }

    static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
    static TMPro.TMP_Text Label(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
        return go.AddComponent<TMPro.TextMeshProUGUI>();
    }
}
