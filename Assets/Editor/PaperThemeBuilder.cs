using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.TextCore.LowLevel;

public static class PaperThemeBuilder
{
    const string Art = "Assets/Sprites/PaperTheme/";
    const string Prefabs = "Assets/Prefabs/PaperTheme/";
    public static readonly Color Ink = new Color(.19f,.18f,.17f);
    public static readonly Color Paper = new Color(.98f,.97f,.94f);
    public static readonly Color Green = new Color(.70f,.81f,.67f);
    public static readonly Color Red = new Color(.87f,.69f,.66f);
    public static TMP_FontAsset Handwriting;
    static Sprite frame;
    static PhysicsMaterial2D wallMaterial;

    public static void PrepareAssets()
    {
        Directory.CreateDirectory(Prefabs);
        ImportSprite(Art + "notebook-paper.png");
        ImportSprite(Art + "pencil-gallows.png");
        string framePath = Art + "pencil-frame.png";
        // UI frames are native nine-slice shapes. The illustration assets remain untouched.
        var texture = new Texture2D(128,128,TextureFormat.RGBA32,false);
        var pixels = new Color[128 * 128];
        for (int y=0;y<128;y++) for(int x=0;x<128;x++)
        {
            float grain = Mathf.PerlinNoise(x*.71f,y*.71f);
            float horizontal = Mathf.Min(Mathf.Abs(y-(5+Mathf.Sin(x*.13f)*.65f)), Mathf.Abs(y-(122+Mathf.Sin(x*.16f)*.6f)));
            float vertical = Mathf.Min(Mathf.Abs(x-(5+Mathf.Sin(y*.12f)*.7f)), Mathf.Abs(x-(122+Mathf.Sin(y*.14f)*.5f)));
            float edge = Mathf.Min(horizontal,vertical);
            Color color = Color.Lerp(new Color(.965f,.953f,.923f), Color.white, grain*.3f);
            if(edge < 1.25f) color = new Color(.22f,.21f,.20f, .65f+grain*.3f);
            if(x<3 || x>125 || y<3 || y>125) color.a=0;
            pixels[y*128+x]=color;
        }
        texture.SetPixels(pixels);texture.Apply();File.WriteAllBytes(framePath,texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);
        ImportSprite(framePath, new Vector4(16,16,16,16));
        frame = AssetDatabase.LoadAssetAtPath<Sprite>(framePath);
        string fontPath = "Assets/Fonts/Paper/PatrickHand SDF.asset";
        Handwriting = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPath);
        if (Handwriting == null)
        {
            var font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/Paper/PatrickHand-Regular.ttf");
            Handwriting = TMP_FontAsset.CreateFontAsset(font,90,9,GlyphRenderMode.SDFAA,2048,2048,AtlasPopulationMode.Dynamic);
            Handwriting.name="Patrick Hand SDF";
            string characters = string.Concat(Enumerable.Range(32,95).Select(i=>(char)i)) + "ÇçĞğİıÖöŞşÜüâîûÂÎÛ…—–’“”•";
            if(!Handwriting.TryAddCharacters(characters,out string missing))
                throw new Exception("Patrick Hand missing glyphs: " + missing);
            AssetDatabase.CreateAsset(Handwriting,fontPath);
            AssetDatabase.AddObjectToAsset(Handwriting.material,Handwriting);
            foreach(var atlas in Handwriting.atlasTextures) AssetDatabase.AddObjectToAsset(atlas,Handwriting);
            Handwriting.atlasPopulationMode=AtlasPopulationMode.Static;
            EditorUtility.SetDirty(Handwriting);
        }
        string materialPath = Art + "PaperWalls.physicsMaterial2D";
        wallMaterial=AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(materialPath);
        if(wallMaterial==null) { wallMaterial=new PhysicsMaterial2D("Paper edges");AssetDatabase.CreateAsset(wallMaterial,materialPath); }
        wallMaterial.friction=.08f;wallMaterial.bounciness=.10f;EditorUtility.SetDirty(wallMaterial);
        foreach(string name in new[]{"KeyButton","LetterSlot","OpponentLetter"})
        {
            var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/"+name+".prefab");
            var clone=UnityEngine.Object.Instantiate(source);
            clone.name="Paper"+name;Style(clone);
            if(name=="KeyButton" && clone.GetComponent<PaperPressFeedback>()==null)clone.AddComponent<PaperPressFeedback>();
            PrefabUtility.SaveAsPrefabAsset(clone,Prefabs+"Paper"+name+".prefab");
            UnityEngine.Object.DestroyImmediate(clone);
        }
        AssetDatabase.SaveAssets();
    }

    static void ImportSprite(string path,Vector4 border=default)
    {
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
        importer.spritePixelsPerUnit=100;importer.spriteBorder=border;importer.spritePivot=new Vector2(.5f,.5f);
        var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);
        settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);
        importer.alphaIsTransparency=true;importer.mipmapEnabled=false;
        importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=2048;importer.SaveAndReimport();
    }

    static void Style(GameObject root)
    {
        foreach(var text in root.GetComponentsInChildren<TMP_Text>(true))
        {
            text.font=Handwriting;text.fontSharedMaterial=Handwriting.material;text.color=Ink;
            text.fontStyle=FontStyles.Normal;
        }
        foreach(var image in root.GetComponentsInChildren<Image>(true))
        {
            // Keep recognizable hint/close icons; replace surfaces and panel borders.
            if(image.GetComponent<Button>()!=null || image.sprite==null || image.sprite.name=="UISprite" || image.sprite.name=="Background" || image.gameObject.name.ToLowerInvariant().Contains("panel") || root.name.StartsWith("Paper"))
            { image.sprite=frame;image.type=Image.Type.Sliced;image.color=Color.white; }
            else if(image.color.r<.5f && image.color.g<.5f) image.color=Paper;
        }
        foreach(var button in root.GetComponentsInChildren<Button>(true))
        {
            var colors=button.colors;colors.normalColor=Color.white;colors.highlightedColor=Paper;
            colors.pressedColor=new Color(.84f,.82f,.77f);colors.selectedColor=Paper;
            colors.disabledColor=new Color(.80f,.79f,.76f,.7f);button.colors=colors;
        }
    }

    public static void ApplyToScene(Scene scene,HangmanDrawer drawer)
    {
        if(frame==null) PrepareAssets();
        var camera=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Camera>(true)).First(c=>c.CompareTag("MainCamera"));
        foreach(var old in scene.GetRootGameObjects().Where(g=>new[]{"Pencil gallows","Pencil ground","Pencil paper card","darağcı"}.Contains(g.name))) old.SetActive(false);
        var root=scene.GetRootGameObjects().FirstOrDefault(g=>g.name=="Paper presentation");
        if(root==null){root=new GameObject("Paper presentation");SceneManager.MoveGameObjectToScene(root,scene);}
        var bounds=GetOrAdd<PaperScreenBounds>(root);
        bounds.sceneCamera=camera;bounds.wallMaterial=wallMaterial;drawer.screenBounds=bounds;
        var presentation=GetOrAdd<PaperScenePresentation>(root);
        presentation.screenBounds=bounds;presentation.drawer=drawer;presentation.ropeAnchor=drawer.ropeTipAnchor;
        // Coordinates measured in the generated image: rope ends at pixel (822,352).
        presentation.ropeTipUV=new Vector2(822f/1024,1-352f/1536);
        presentation.beamUV=new Vector2(822f/1024,1-110f/1536);
        presentation.postUV=new Vector2(201f/1024,1-110f/1536);
        presentation.paper=AddSprite(root,"Notebook paper",Art+"notebook-paper.png",-100);
        presentation.gallows=AddSprite(root,"Graphite gallows",Art+"pencil-gallows.png",105);
        bounds.RefreshForViewport(new Rect(0,0,1,1),720f/1560);presentation.RefreshPresentation();
        foreach(var sceneRoot in scene.GetRootGameObjects())
        {
            if(sceneRoot.GetComponent<Canvas>()!=null)
            {
                var canvas=sceneRoot.GetComponent<Canvas>();
                canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=20;canvas.sortingOrder=200;
                Style(sceneRoot);
                if(sceneRoot.transform.Find("Paper safe area")==null)
                {
                    var children=sceneRoot.transform.Cast<Transform>().ToArray();
                    var safe=new GameObject("Paper safe area",typeof(RectTransform),typeof(PaperSafeArea));
                    safe.transform.SetParent(sceneRoot.transform,false);
                    var safeRect=safe.GetComponent<RectTransform>();safeRect.anchorMin=Vector2.zero;safeRect.anchorMax=Vector2.one;safeRect.offsetMin=safeRect.offsetMax=Vector2.zero;
                    foreach(var child in children) child.SetParent(safe.transform,false);
                }
            }
            foreach(var keyboard in sceneRoot.GetComponentsInChildren<KeyboardUI>(true))
            {
                keyboard.keyButtonPrefab=AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs+"PaperKeyButton.prefab");
                keyboard.defaultColor=Color.white;keyboard.correctColor=Green;keyboard.wrongColor=Red;keyboard.presentColor=new Color(.88f,.83f,.65f);
            }
            foreach(var word in sceneRoot.GetComponentsInChildren<WordDisplay>(true))
            {
                word.letterSlotPrefab=AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs+"PaperLetterSlot.prefab");
                word.hiddenSlotColor=Color.white;word.correctLetterColor=Green;word.unrevealedLetterColor=Red;
                PrepareWordRow(word.letterContainer);
            }
            foreach(var multiplayer in sceneRoot.GetComponentsInChildren<MultiplayerGameManager>(true))
            {
                multiplayer.myBoxPrefab=AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs+"PaperLetterSlot.prefab");
                multiplayer.opponentBoxPrefab=AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs+"PaperOpponentLetter.prefab");
                multiplayer.hiddenSlotColor=Color.white;multiplayer.correctLetterColor=Green;
                if(multiplayer.opponentNameText!=null) multiplayer.opponentNameText.text="Rakip bekleniyor…";
                PrepareWordRow(multiplayer.myWordContainer);
                PrepareWordRow(multiplayer.opponentWordContainer);
            }
            foreach(var ui in sceneRoot.GetComponentsInChildren<GameUI>(true))
            { ui.winColor=new Color(.27f,.43f,.26f);ui.loseColor=new Color(.57f,.29f,.26f); }
        }
        PaperInteractionBuilder.Apply(scene);
    }

    static void PrepareWordRow(Transform row)
    {
        var rect=row.GetComponent<RectTransform>();
        var min=rect.anchorMin;var max=rect.anchorMax;min.x=.08f;max.x=.92f;rect.anchorMin=min;rect.anchorMax=max;
        rect.anchoredPosition=new Vector2(0,rect.anchoredPosition.y);rect.sizeDelta=new Vector2(0,rect.sizeDelta.y);
        var fitter=row.GetComponent<ContentSizeFitter>();if(fitter!=null) fitter.horizontalFit=ContentSizeFitter.FitMode.Unconstrained;
        var layout=row.GetComponent<HorizontalLayoutGroup>();
        if(layout!=null){layout.spacing=8;layout.childControlWidth=true;layout.childForceExpandWidth=false;layout.childAlignment=TextAnchor.MiddleCenter;}
        if(row.GetComponent<PaperWordLayout>()==null) row.gameObject.AddComponent<PaperWordLayout>();
    }

    static T GetOrAdd<T>(GameObject go) where T : Component
    {
        var component=go.GetComponent<T>();
        return component!=null ? component : go.AddComponent<T>();
    }

    static SpriteRenderer AddSprite(GameObject parent,string name,string asset,int order)
    {
        var child=parent.transform.Find(name);
        if(child==null){var go=new GameObject(name);go.transform.SetParent(parent.transform,false);child=go.transform;}
        var renderer=GetOrAdd<SpriteRenderer>(child.gameObject);
        renderer.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(asset);renderer.sortingOrder=order;
        renderer.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat");
        return renderer;
    }
}
