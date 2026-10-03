using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class PencilRagdollBuilder
{
    public const string Folder = "Assets/Sprites/PencilStickman";
    public const string PrefabPath = "Assets/Prefabs/PencilRagdoll.prefab";
    public const string PreviewPath = "Assets/Scenes/PencilRagdollPreview.unity";
    const string Output = ".utmp/pencil-validation";
    static readonly string[] Names = { "head", "torso", "left-arm", "right-arm", "left-leg", "right-leg" };
    static readonly float[] Lengths = { 0.6f, 0.68f, 0.64f, 0.64f, 0.82f, 0.82f };
    static Material spriteMaterial;

    class Art
    {
        public Sprite sprite;
        public Vector2 top;
        public Vector2 bottom;
    }

    [MenuItem("Tools/Pencil Stickman/1 Build and validate _F8")]
    public static void BuildAndValidate()
    {
        Directory.CreateDirectory(Output);
        try
        {
            Art[] art = ImportParts();
            PencilDrawingAssets.Build();
            PaperThemeBuilder.PrepareAssets();
            GameObject model = CreateRagdoll(art);
            try { PrefabUtility.SaveAsPrefabAsset(model, PrefabPath); }
            finally { UnityEngine.Object.DestroyImmediate(model); }
            AssetDatabase.SaveAssets();
            ValidatePhysics();
            CreatePreviewScene();
            if (File.Exists(Output + "/build-error.txt")) File.Delete(Output + "/build-error.txt");
            File.WriteAllText(Output + "/build-success.txt", DateTime.Now.ToString("O"));
            Debug.Log("[PencilStickman] Six sprites, prefab, physics validation and preview scene are ready.");
        }
        catch (Exception ex)
        {
            File.WriteAllText(Output + "/build-error.txt", ex.ToString());
            Debug.LogException(ex);
        }
    }

    static Art[] ImportParts()
    {
        var source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        source.LoadImage(File.ReadAllBytes(Folder + "/parts-atlas.png"));
        Color32[] pixels = source.GetPixels32();
        int cellWidth = source.width / 3, cellHeight = source.height / 2;
        Art[] result = new Art[6];
        for (int i = 0; i < 6; i++)
        {
            int left = (i % 3) * cellWidth, bottom = (1 - i / 3) * cellHeight;
            int minX = cellWidth, minY = cellHeight, maxX = -1, maxY = -1;
            for (int y = 0; y < cellHeight; y++)
                for (int x = 0; x < cellWidth; x++)
                    if (pixels[(bottom + y) * source.width + left + x].a >= 64)
                    { minX = Math.Min(minX, x); maxX = Math.Max(maxX, x); minY = Math.Min(minY, y); maxY = Math.Max(maxY, y); }
            if (maxX < minX) throw new Exception("Empty sprite cell: " + Names[i]);
            minX = Math.Max(0, minX - 4); minY = Math.Max(0, minY - 4);
            maxX = Math.Min(cellWidth - 1, maxX + 4); maxY = Math.Min(cellHeight - 1, maxY + 4);
            int width = maxX - minX + 1, height = maxY - minY + 1;
            var crop = new Texture2D(width, height, TextureFormat.RGBA32, false);
            // Sprite extraction only: original generated RGBA pixels remain unchanged.
            var partPixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
                Array.Copy(pixels, (bottom + minY + y) * source.width + left + minX, partPixels, y * width, width);
            crop.SetPixels32(partPixels); crop.Apply();
            string path = Folder + "/" + Names[i] + ".png";
            File.WriteAllBytes(path, crop.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(crop);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();
            result[i] = new Art {
                sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path),
                top = StrokeCenter(partPixels, width, height, true),
                bottom = StrokeCenter(partPixels, width, height, false)
            };
        }
        UnityEngine.Object.DestroyImmediate(source);
        return result;
    }

    static Vector2 StrokeCenter(Color32[] pixels, int width, int height, bool upper)
    {
        int first = upper ? height - 1 : 0, step = upper ? -1 : 1;
        for (int row = first; row >= 0 && row < height; row += step)
        {
            float sumX = 0, sumY = 0, weight = 0;
            for (int dy = 0; dy < 7; dy++)
            {
                int y = row + dy * step;
                if (y < 0 || y >= height) continue;
                for (int x = 0; x < width; x++)
                {
                    byte alpha = pixels[y * width + x].a;
                    if (alpha < 96) continue;
                    sumX += x * alpha; sumY += y * alpha; weight += alpha;
                }
            }
            if (weight > 0) return new Vector2(sumX / weight, sumY / weight);
        }
        throw new Exception("No pencil endpoint found.");
    }

    static Material SpriteMaterial()
    {
        if (spriteMaterial == null)
        {
            spriteMaterial = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat");
            if (spriteMaterial == null) throw new Exception("URP Sprite-Unlit-Default material is missing.");
        }
        return spriteMaterial;
    }

    static GameObject CreateRagdoll(Art[] art)
    {
        var root = new GameObject("PencilRagdoll");
        root.SetActive(false);
        var controller = root.AddComponent<RagdollController>();
        var effect = root.AddComponent<PencilDrawEffect>();
        controller.drawingEffect = effect;
        var tip = new GameObject("Drawing pencil");
        tip.transform.SetParent(root.transform, false);
        effect.pencilTip = tip.AddComponent<SpriteRenderer>();
        effect.pencilTip.sprite = PencilDrawingAssets.Pencil;
        effect.pencilTip.sharedMaterial = SpriteMaterial();
        tip.transform.localScale = Vector3.one * (0.30f / effect.pencilTip.sprite.bounds.size.y);
        effect.pencilTip.enabled = false;
        effect.pencilSound = root.AddComponent<AudioSource>();
        effect.pencilSound.clip = PencilDrawingAssets.Scratch;
        effect.writingVariations = PencilDrawingAssets.WritingVariations;
        effect.pencilSound.playOnAwake = false;
        effect.pencilSound.spatialBlend = 0;
        effect.pencilSound.volume = 0;
        controller.partsOrder = new RagdollController.BodyPart[6];
        Vector2[] positions = { new Vector2(0,-0.3f), new Vector2(0,-0.6f), new Vector2(-0.018f,-0.635f), new Vector2(0.018f,-0.635f), new Vector2(-0.012f,-1.28f), new Vector2(0.012f,-1.28f) };
        float[] angles = { 0, 0, -32, 32, -18, 18 };
        float[] masses = { 0.3f, 0.18f, 0.085f, 0.085f, 0.12f, 0.12f };
        for (int i = 0; i < 6; i++)
        {
            var part = new GameObject(Names[i]);
            part.transform.SetParent(root.transform, false);
            part.transform.localPosition = positions[i];
            part.transform.localRotation = Quaternion.Euler(0,0,angles[i]);
            var body = part.AddComponent<Rigidbody2D>();
            body.mass = masses[i]; body.gravityScale = 1;
            body.linearDamping = 0.45f; body.angularDamping = 2.3f;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            Collider2D collider;
            if (i == 0)
            {
                var circle = part.AddComponent<CircleCollider2D>(); circle.radius = 0.29f; collider = circle;
            }
            else
            {
                var capsule = part.AddComponent<CapsuleCollider2D>();
                capsule.direction = CapsuleDirection2D.Vertical;
                capsule.size = new Vector2(i == 1 ? 0.07f : 0.09f, Lengths[i]);
                capsule.offset = new Vector2(0,-Lengths[i]/2); collider = capsule;
            }
            var joint = part.AddComponent<HingeJoint2D>();
            joint.autoConfigureConnectedAnchor = false;
            joint.anchor = Vector2.zero;
            joint.enableCollision = false;
            joint.useLimits = true;
            if (i == 0)
            {
                joint.anchor = new Vector2(0,0.3f); joint.connectedAnchor = Vector2.zero;
                joint.limits = new JointAngleLimits2D { min=-65,max=65 };
            }
            else
            {
                var parent = controller.partsOrder[i == 1 ? 0 : 1].body;
                joint.connectedBody = parent;
                joint.anchor = Vector2.zero;
                joint.connectedAnchor = parent.transform.InverseTransformPoint(part.transform.position);
                joint.limits = i == 1 ? new JointAngleLimits2D {min=-14,max=14}
                    : i == 2 ? new JointAngleLimits2D {min=-28,max=8}
                    : i == 3 ? new JointAngleLimits2D {min=-8,max=28}
                    : i == 4 ? new JointAngleLimits2D {min=-22,max=0}
                    : new JointAngleLimits2D {min=0,max=22};
            }
            var visual = new GameObject("Pencil artwork");
            visual.transform.SetParent(part.transform, false);
            var renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = art[i].sprite;
            renderer.sharedMaterial = PencilDrawingAssets.StrokeMaterial;
            renderer.sortingOrder = i == 0 ? 113 : (i == 1 ? 112 : 111);
            if (i == 0)
            {
                float scale = Lengths[0] / renderer.sprite.bounds.size.x;
                visual.transform.localScale = Vector3.one * scale;
                // Hair is above the circle; keep the actual chin aligned with the neck.
                visual.transform.localPosition = new Vector3(0,0.008f,0);
            }
            else
            {
                Vector2 direction = art[i].bottom - art[i].top;
                // Keep the feet's outward bend; aligning to the toe would turn the shafts inward.
                float angle = i >= 4 ? 0f : Vector2.SignedAngle(direction, Vector2.down);
                float scale = Lengths[i] * 100f / (i >= 4 ? Mathf.Abs(direction.y) : direction.magnitude);
                visual.transform.localRotation = Quaternion.Euler(0,0,angle);
                visual.transform.localScale = Vector3.one * scale;
                Vector2 pivot = renderer.sprite.pivot;
                Vector2 offset = (pivot - art[i].top) / 100f;
                visual.transform.localPosition = Quaternion.Euler(0,0,angle) * (offset * scale);
            }
            if (i >= 4)
            {
                Vector2 heel = i == 4 ? new Vector2(85, art[i].sprite.rect.height - 338) : new Vector2(43, art[i].sprite.rect.height - 341);
                Vector2 toe = i == 4 ? new Vector2(13, art[i].sprite.rect.height - 369) : new Vector2(112, art[i].sprite.rect.height - 371);
                Vector2 a = part.transform.InverseTransformPoint(visual.transform.TransformPoint((heel - renderer.sprite.pivot) / 100));
                Vector2 b = part.transform.InverseTransformPoint(visual.transform.TransformPoint((toe - renderer.sprite.pivot) / 100));
                var foot = part.AddComponent<BoxCollider2D>();
                foot.offset = (a + b) / 2;
                foot.size = new Vector2(Mathf.Abs(b.x - a.x) + .055f, Mathf.Abs(b.y - a.y) + .055f);
            }
            var interaction = part.AddComponent<HangmanPart>();
            interaction.dragForce = 18;
            controller.partsOrder[i] = new RagdollController.BodyPart { spriteRenderer=renderer, partCollider=collider, body=body, joint=joint, strokeProfile=PencilDrawingAssets.Profiles[i] };
        }
        root.SetActive(true);
        return root;
    }

    static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }

    static void LayerAll(GameObject root, int layer)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = layer;
    }

    public static void ValidatePhysics()
    {
        Directory.CreateDirectory(Output);
        var scene = EditorSceneManager.NewPreviewScene();
        try
        {
            var anchor = new GameObject("Test rope anchor");
            SceneManager.MoveGameObjectToScene(anchor, scene);
            anchor.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
            var model = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath));
            SceneManager.MoveGameObjectToScene(model, scene); LayerAll(model,31);
            Physics2D.SyncTransforms();
            var rig = model.GetComponent<RagdollController>(); rig.InitializeParts(); rig.AttachToRope(anchor.transform);
            foreach (var part in rig.partsOrder) part.body.interpolation = RigidbodyInterpolation2D.None;
            var physics = scene.GetPhysicsScene2D();
            Require(physics.IsValid() && physics != Physics2D.defaultPhysicsScene, "Preview physics is not isolated.");
            float maxError = 0, maxSpeed = 0;
            for (int i = 0; i < 6; i++)
            {
                Require(!rig.partsOrder[i].body.simulated, "A hidden part is affecting physics.");
                Require(!rig.partsOrder[i].partCollider.enabled, "Hidden collider is enabled.");
            }
            rig.RevealPart(0,false); rig.RevealPart(7,false);
            Require(rig.RevealedCount == 0, "Out-of-range reveal changed the rig.");
            for (int stage = 1; stage <= 6; stage++)
            {
                rig.RevealPart(stage,false); rig.RevealPart(stage,false);
                Require(rig.RevealedCount == stage, "Repeated reveal is not idempotent.");
                for (int i = 0; i < 6; i++)
                    Require(rig.partsOrder[i].body.simulated == (i < stage), "Wrong simulated body at stage " + stage);
                for (int step = 0; step < 40; step++)
                {
                    physics.Simulate(0.02f);
                    maxError = Mathf.Max(maxError, JointError(rig));
                }
                Render(model,scene,Output + "/stage-" + stage + ".png");
                if (stage == 2) rig.AddImpulse(new Vector2(0.07f,0));
            }
            foreach (var part in rig.partsOrder)
            {
                Require(part.body.transform.localScale == Vector3.one, "The physics root was scaled.");
                Require(part.spriteRenderer.transform.localScale == part.originalScale, "Reveal changed artwork scale.");
                part.body.AddForce(new Vector2(0.04f,0),ForceMode2D.Impulse);
            }
            // Same constrained spring used by mouse/touch dragging in HangmanPart.
            var pulled = rig.partsOrder[2].body;
            var drag = pulled.gameObject.AddComponent<TargetJoint2D>();
            drag.autoConfigureTarget = false;
            drag.anchor = new Vector2(0,-0.4f);
            drag.target = pulled.GetRelativePoint(drag.anchor) + new Vector2(-0.7f,0.2f);
            drag.frequency = 4f; drag.dampingRatio = 0.85f;
            drag.maxForce = pulled.mass * 18f;
            for (int step = 0; step < 50; step++)
            {
                physics.Simulate(0.02f);
                maxError = Mathf.Max(maxError,JointError(rig));
            }
            UnityEngine.Object.DestroyImmediate(drag);
            for (int step = 0; step < 600; step++)
            {
                physics.Simulate(0.02f);
                maxError = Mathf.Max(maxError,JointError(rig));
                foreach (var part in rig.partsOrder)
                {
                    Require(!float.IsNaN(part.body.position.x) && !float.IsNaN(part.body.position.y), "Invalid position.");
                    maxSpeed = Mathf.Max(maxSpeed,part.body.linearVelocity.magnitude);
                }
                if (step == 25) Render(model,scene,Output + "/swing.png");
            }
            float remainingSpeed = rig.partsOrder.Max(p => p.body.linearVelocity.magnitude);
            File.WriteAllText(Output + "/physics-debug.txt",string.Join("\n",rig.partsOrder.Select(p => p.body.name + " position=" + p.body.position + " angle=" + p.body.rotation + " reference=" + p.joint.referenceAngle + " jointAngle=" + p.joint.jointAngle + " restAngle=" + p.restAngle + " limits=" + p.joint.limits.min + "," + p.joint.limits.max)));
            Require(maxError < 0.045f, "Joint separation exceeds tolerance: " + maxError);
            Require(remainingSpeed < 0.12f, "Rig is not settling: " + remainingSpeed);
            Require(Mathf.DeltaAngle(rig.partsOrder[1].body.rotation,rig.partsOrder[2].body.rotation) < -18f,"Left arm collapses into torso or crosses sides.");
            Require(Mathf.DeltaAngle(rig.partsOrder[1].body.rotation,rig.partsOrder[3].body.rotation) > 18f,"Right arm collapses into torso or crosses sides.");
            Require(Mathf.DeltaAngle(rig.partsOrder[1].body.rotation,rig.partsOrder[4].body.rotation) < -10f,"Left leg collapses into torso or crosses sides.");
            Require(Mathf.DeltaAngle(rig.partsOrder[1].body.rotation,rig.partsOrder[5].body.rotation) > 10f,"Right leg collapses into torso or crosses sides.");
            Render(model,scene,Output + "/assembled.png");
            File.WriteAllText(Output + "/physics-results.txt",
                "PASS: six independent bodies/sprites; hidden physics disabled; reveal stages 1-6; repeated/out-of-range reveal; moving-parent alignment; unit physics scales; constrained drag; 12 seconds of impulse/settling simulation; separated arm/leg resting pose.\n" +
                "Max joint gap: " + maxError.ToString("F6") + " units\nMax speed: " + maxSpeed.ToString("F4") +
                " units/s\nFinal speed: " + remainingSpeed.ToString("F6") + " units/s");
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }

    static float JointError(RagdollController rig)
    {
        float result = 0;
        foreach (var part in rig.partsOrder)
        {
            if (!part.revealed || part.joint == null) continue;
            Vector2 here = part.body.GetRelativePoint(part.joint.anchor);
            Vector2 there = part.joint.connectedBody != null ? part.joint.connectedBody.GetRelativePoint(part.joint.connectedAnchor) : part.joint.connectedAnchor;
            result = Mathf.Max(result,Vector2.Distance(here,there));
        }
        return result;
    }

    public static void Render(GameObject model, Scene scene, string path)
    {
        var cameraObject = new GameObject("Validation camera");
        SceneManager.MoveGameObjectToScene(cameraObject,scene);
        var camera = cameraObject.AddComponent<Camera>();
        camera.scene = scene;
        camera.transform.position = model.transform.position + new Vector3(0,-1.02f,-10);
        camera.orthographic = true; camera.orthographicSize = 1.37f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.97f,0.953f,0.91f);
        camera.cullingMask = 1 << 31;
        var texture = new RenderTexture(720,960,24);
        camera.targetTexture = texture;
        var previous = RenderTexture.active;
        try
        {
            camera.Render();
            RenderTexture.active = texture;
            var image = new Texture2D(720,960,TextureFormat.RGB24,false);
            image.ReadPixels(new Rect(0,0,720,960),0,0); image.Apply();
            File.WriteAllBytes(path,image.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(image);
        }
        finally
        {
            RenderTexture.active = previous;
            camera.targetTexture = null;
            texture.Release(); UnityEngine.Object.DestroyImmediate(texture);
            UnityEngine.Object.DestroyImmediate(cameraObject);
        }
    }

    static void CreatePreviewScene()
    {
        var scene = SceneManager.GetSceneByPath(PreviewPath);
        bool alreadyLoaded = scene.IsValid() && scene.isLoaded;
        if (alreadyLoaded)
        {
            Require(!scene.isDirty,"Save workshop changes before regenerating it.");
            foreach (var root in scene.GetRootGameObjects()) UnityEngine.Object.DestroyImmediate(root);
        }
        else scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
        try
        {
            var cameraObject = new GameObject("Main Camera"); SceneManager.MoveGameObjectToScene(cameraObject,scene);
            var camera = cameraObject.AddComponent<Camera>(); camera.tag = "MainCamera";
            cameraObject.AddComponent<AudioListener>();
            camera.transform.position = new Vector3(-0.2f,-0.8f,-10);
            camera.orthographic = true; camera.orthographicSize = 1.8f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.97f,0.953f,0.91f);
            var lightObject = new GameObject("Workshop Light"); SceneManager.MoveGameObjectToScene(lightObject,scene);
            lightObject.AddComponent<Light>().type = LightType.Directional;
            var anchor = new GameObject("RopeTip"); SceneManager.MoveGameObjectToScene(anchor,scene);
            anchor.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
            AddLine(scene,"Pencil gallows",new[] { new Vector3(-0.95f,-2.23f,0),new Vector3(-0.95f,0.48f,0),new Vector3(0,0.48f,0),Vector3.zero },0.016f);
            AddLine(scene,"Pencil ground",new[] { new Vector3(-1.25f,-2.23f,0),new Vector3(0.9f,-2.23f,0) },0.014f);
            var host = new GameObject("Drawing workshop"); SceneManager.MoveGameObjectToScene(host,scene);
            var drawer = host.AddComponent<HangmanDrawer>();
            drawer.partInterval = 0.08f;
            drawer.ragdollPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath); drawer.ropeTipAnchor = anchor.transform;
            host.AddComponent<PencilRagdollPreview>().drawer = drawer;
            PaperThemeBuilder.ApplyToScene(scene, drawer);
            var pose = (GameObject)PrefabUtility.InstantiatePrefab(drawer.ragdollPrefab,scene);
            pose.name = "Editor pose (hidden during Play)";
            pose.transform.position = drawer.ropeTipAnchor.position;
            // The workshop replaces this edit-time pose with a fresh revealable rig when entering Play.
            pose.AddComponent<PencilPreviewPose>();
            EditorSceneManager.SaveScene(scene,PreviewPath);
        }
        finally { if (!alreadyLoaded) EditorSceneManager.CloseScene(scene,true); }
    }

    static void AddLine(Scene scene,string name,Vector3[] points,float width)
    {
        var go = new GameObject(name); SceneManager.MoveGameObjectToScene(go,scene);
        var line = go.AddComponent<LineRenderer>();
        line.sharedMaterial = SpriteMaterial(); line.positionCount = points.Length; line.SetPositions(points);
        line.startWidth = width; line.endWidth = width; line.numCapVertices = 4;
        line.startColor = line.endColor = new Color(0.2f,0.18f,0.16f);
        line.sortingOrder = 10;
    }

    [MenuItem("Tools/Pencil Stickman/2 Apply to game scenes %#F9")]
    public static void ApplyToScenes()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        Require(prefab != null,"Build the pencil prefab first.");
        foreach (string path in new[] { "Assets/Scenes/GAMESCENE.unity", "Assets/Scenes/MultiplayerGameScene.unity" })
        {
            Scene scene = SceneManager.GetSceneByPath(path);
            bool alreadyLoaded = scene.IsValid() && scene.isLoaded;
            if (!alreadyLoaded) scene = EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            try
            {
                Require(!scene.isDirty,"Unsaved scene changes exist: " + path);
                var drawer = scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<HangmanDrawer>(true)).First();
                drawer.ragdollPrefab = prefab;
                drawer.partInterval = 0.08f;
                if (drawer.ropeTipAnchor == null)
                {
                    var anchor = new GameObject("RopeTip");
                    SceneManager.MoveGameObjectToScene(anchor,scene);
                    anchor.transform.position = new Vector3(-1.25f,4.35f,0);
                    anchor.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
                    drawer.ropeTipAnchor = anchor.transform;
                    var oldImage = drawer.GetComponent<UnityEngine.UI.RawImage>();
                    if (oldImage != null) { oldImage.enabled = false; oldImage.raycastTarget = false; }
                }
                if (!scene.GetRootGameObjects().Any(g=>g.name=="Pencil gallows"))
                {
                    Vector3 tip = drawer.ropeTipAnchor.position;
                    AddLine(scene,"Pencil gallows",new[] { tip+new Vector3(-0.87f,-2.16f,0),tip+new Vector3(-0.87f,0.3f,0),tip+new Vector3(0,0.3f,0),tip },0.014f);
                }
                PaperThemeBuilder.ApplyToScene(scene, drawer);
                foreach (var root in scene.GetRootGameObjects())
                {
                    if (root.name == "darağcı")
                        root.SetActive(false);
                    if (root.name == "Pencil gallows") root.GetComponent<LineRenderer>().sortingOrder = 105;
                }
                EditorUtility.SetDirty(drawer);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                CaptureInstalledScene(scene,drawer);
            }
            finally { if (!alreadyLoaded) EditorSceneManager.CloseScene(scene,true); }
        }
        File.WriteAllText(Output + "/scene-integration.txt","PASS: GAMESCENE and MultiplayerGameScene reference PencilRagdoll with rope anchors.");
        Debug.Log("[PencilStickman] Pencil character installed in both game scenes.");
    }

    static void CaptureInstalledScene(Scene scene,HangmanDrawer drawer)
    {
        var camera = scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Camera>()).First(c=>c.CompareTag("MainCamera"));
        var pose = UnityEngine.Object.Instantiate(drawer.ragdollPrefab,drawer.ropeTipAnchor.position,Quaternion.identity);
        SceneManager.MoveGameObjectToScene(pose,scene);
        var rig = pose.GetComponent<RagdollController>(); rig.AttachToRope(drawer.ropeTipAnchor);
        for (int i=1;i<=6;i++) rig.RevealPart(i,false);
        Physics2D.SyncTransforms();
        var objects=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Select(t=>t.gameObject).ToArray();
        var layers=objects.Select(g=>g.layer).ToArray();
        foreach (var item in objects) item.layer=31;
        var canvases = scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Canvas>()).Where(c=>c.renderMode==RenderMode.ScreenSpaceOverlay).ToArray();
        foreach (var canvas in canvases) { canvas.renderMode=RenderMode.ScreenSpaceCamera; canvas.worldCamera=camera; }
        var oldTarget=camera.targetTexture; var oldScene=camera.scene; var oldActive=RenderTexture.active; var oldMask=camera.cullingMask;
        var target=new RenderTexture(720,1560,24);
        try
        {
            camera.scene=scene; camera.cullingMask=1<<31; camera.targetTexture=target; camera.Render(); RenderTexture.active=target;
            var image=new Texture2D(720,1560,TextureFormat.RGB24,false);
            image.ReadPixels(new Rect(0,0,720,1560),0,0); image.Apply();
            File.WriteAllBytes(Output+"/installed-"+scene.name+".png",image.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(image);
        }
        finally
        {
            camera.scene=oldScene; camera.cullingMask=oldMask; camera.targetTexture=oldTarget; RenderTexture.active=oldActive;
            for(int i=0;i<objects.Length;i++) objects[i].layer=layers[i];
            foreach (var canvas in canvases) { canvas.worldCamera=null; canvas.renderMode=RenderMode.ScreenSpaceOverlay; }
            target.Release(); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(pose);
        }
    }

    static void AddPaperCard(Scene scene,Vector3 tip)
    {
        string path = Folder + "/paper-card.png";
        if (!File.Exists(path))
        {
            var image = new Texture2D(2,2,TextureFormat.RGBA32,false);
            image.SetPixels(Enumerable.Repeat(new Color(0.97f,0.953f,0.91f),4).ToArray()); image.Apply();
            File.WriteAllBytes(path,image.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(image);
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite; importer.spritePixelsPerUnit = 2;
            importer.textureCompression = TextureImporterCompression.Uncompressed; importer.SaveAndReimport();
        }
        var existing = scene.GetRootGameObjects().FirstOrDefault(g=>g.name=="Pencil paper card");
        var card = existing != null ? existing : new GameObject("Pencil paper card");
        if (existing == null) SceneManager.MoveGameObjectToScene(card,scene);
        card.transform.position = tip + new Vector3(-0.18f,-1.04f,0.1f);
        card.transform.localScale = new Vector3(2.22f,2.49f,1);
        var renderer = card.GetComponent<SpriteRenderer>();
        if (renderer == null) renderer = card.AddComponent<SpriteRenderer>();
        renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        renderer.sharedMaterial = SpriteMaterial(); renderer.sortingOrder = 100;
    }

    [MenuItem("Tools/Pencil Stickman/3 Open workshop _F10")]
    public static void OpenWorkshop()
    {
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            EditorSceneManager.OpenScene(PreviewPath);
    }
}
