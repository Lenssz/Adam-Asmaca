using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class PaperMenuBuilder
{
    public const string ScenePath="Assets/Scenes/SampleScene.unity";
    public const string Output=".utmp/menu-validation";
    static TMP_FontAsset font;
    static Sprite frame;

    [MenuItem("Tools/Pencil Stickman/5 Apply paper main menu")]
    public static void Build()
    {
        var scene=SceneManager.GetSceneByPath(ScenePath);
        bool loaded=scene.IsValid() && scene.isLoaded;
        if(!loaded)scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
        try
        {
            if(scene.isDirty)throw new Exception("Save main menu changes before applying its paper design.");
            font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Paper/PatrickHand SDF.asset");
            frame=CreateOutline();
            if(font==null || frame==null)throw new Exception("Build the paper game assets first.");
            var all=scene.GetRootGameObjects();
            var canvas=all.SelectMany(g=>g.GetComponentsInChildren<Canvas>(true)).First();
            var lobby=all.SelectMany(g=>g.GetComponentsInChildren<LobbyManager>(true)).First();
            string before=Bindings(canvas.gameObject);
            var scaler=canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1080,1920);scaler.matchWidthOrHeight=.5f;
            canvas.renderMode=RenderMode.ScreenSpaceCamera;
            canvas.worldCamera=all.SelectMany(g=>g.GetComponentsInChildren<Camera>()).First(c=>c.CompareTag("MainCamera"));
            canvas.planeDistance=20;canvas.sortingOrder=200;
            var background=canvas.transform.Find("Menu notebook paper");
            if(background==null)background=new GameObject("Menu notebook paper",typeof(RectTransform),typeof(Image)).transform;
            background.SetParent(canvas.transform,false);background.SetAsFirstSibling();
            Stretch((RectTransform)background);
            var paper=background.GetComponent<Image>();paper.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/PaperTheme/notebook-paper.png");paper.color=Color.white;paper.raycastTarget=false;
            var safe=canvas.transform.Find("Menu safe area");
            if(safe==null)
            {
                var children=canvas.transform.Cast<Transform>().Where(t=>t!=background).ToArray();
                safe=new GameObject("Menu safe area",typeof(RectTransform),typeof(PaperSafeArea)).transform;
                safe.SetParent(canvas.transform,false);Stretch((RectTransform)safe);
                foreach(var child in children)child.SetParent(safe,false);
            }
            Style(safe.gameObject);
            foreach(var text in safe.GetComponentsInChildren<TMP_Text>(true).Where(t=>t.text.Trim()=="ADAM ASMACA"))
            {
                if(text.transform.IsChildOf(lobby.modeSelectionPanel.transform))
                {text.text="Bir defter dolusu kelime";Place(text.rectTransform,.5f,.79f,920,75);text.fontSize=38;}
                else{Place(text.rectTransform,.5f,.87f,960,160);text.fontSize=100;}
            }
            Stretch(lobby.modeSelectionPanel.GetComponent<RectTransform>());
            var modeImage=lobby.modeSelectionPanel.GetComponent<Image>();if(modeImage!=null){modeImage.enabled=false;modeImage.raycastTarget=false;}
            foreach(var button in lobby.modeSelectionPanel.GetComponentsInChildren<Button>(true))
            {
                var label=button.GetComponentInChildren<TMP_Text>();if(label==null)continue;
                string text=label.text.Trim().ToUpperInvariant();
                if(text.Contains("TEK")){Place((RectTransform)button.transform,.5f,.37f,840,145);label.text="Tek Oyunculu";}
                else if(text.Contains("ÇOK")){Place((RectTransform)button.transform,.5f,.25f,840,145);label.text="Çok Oyunculu";}
                else if(text.Contains("PROF")){Place((RectTransform)button.transform,.5f,.13f,480,100);label.text="Profilim";}
                label.fontSize=54;label.enableAutoSizing=false;Stretch(label.rectTransform);
            }
            CreateCharacter(lobby.modeSelectionPanel.transform);
            Label(lobby.modeSelectionPanel.transform,"Menu footer","Tahmin et. Çizimi tamamla.",.5f,.045f,900,55,30);
            var coin=all.SelectMany(g=>g.GetComponentsInChildren<CoinDisplay>(true)).FirstOrDefault();
            if(coin!=null && coin.coinText!=null){Place(coin.coinText.rectTransform,.80f,.965f,330,70);coin.coinText.fontSize=34;coin.coinText.alignment=TextAlignmentOptions.Right;}
            Stretch(lobby.categoryPanel.GetComponent<RectTransform>());
            var categorySurface=lobby.categoryPanel.GetComponent<Image>();categorySurface.enabled=false;categorySurface.raycastTarget=false;
            var backdropButton=lobby.categoryPanel.GetComponent<Button>();if(backdropButton!=null)backdropButton.enabled=false;
            Label(lobby.categoryPanel.transform,"Category heading","Bir kategori seç",.5f,.76f,900,80,42);
            string[] categories={"League of Legends","Valorant","Counter-Strike","Genel Kültür","Ülkeler","Minecraft"};
            string[] logos={"league","valorant","counter-strike","general","countries","minecraft"};
            foreach(var button in lobby.categoryPanel.GetComponentsInChildren<Button>(true).Where(b=>b!=backdropButton))
            {
                var serialized=new SerializedObject(button);
                var calls=serialized.FindProperty("m_OnClick.m_PersistentCalls.m_Calls");
                if(calls.arraySize==0)continue;
                var call=calls.GetArrayElementAtIndex(0);
                if(call.FindPropertyRelative("m_MethodName").stringValue!="OnCategoryButtonClicked")continue;
                int index=call.FindPropertyRelative("m_Arguments.m_IntArgument").intValue;
                Place((RectTransform)button.transform,index%2==0?.27f:.73f,.63f-(index/2)*.20f,340,340);
                var label=button.GetComponentInChildren<TMP_Text>();label.text=categories[index];label.fontSize=36;label.enableAutoSizing=true;label.fontSizeMin=28;label.fontSizeMax=36;Place(label.rectTransform,.5f,.115f,316,58);label.margin=Vector4.zero;label.raycastTarget=false;
                var existing=button.transform.Find("Pencil category logo");
                var logo=existing!=null ? existing.GetComponent<Image>() : new GameObject("Pencil category logo",typeof(RectTransform),typeof(Image)).GetComponent<Image>();
                logo.transform.SetParent(button.transform,false);Place(logo.rectTransform,.5f,.58f,252,252);
                string logoPath="Assets/Sprites/PaperTheme/MenuLogos/"+logos[index]+"-pencil.png";
                var logoImporter=(TextureImporter)AssetImporter.GetAtPath(logoPath);
                if(logoImporter==null)throw new Exception("Missing pencil category logo: "+logos[index]);
                logoImporter.textureType=TextureImporterType.Sprite;logoImporter.spriteImportMode=SpriteImportMode.Single;
                logoImporter.spritePixelsPerUnit=100;logoImporter.alphaIsTransparency=true;logoImporter.mipmapEnabled=false;
                logoImporter.maxTextureSize=1024;logoImporter.textureCompression=TextureImporterCompression.Uncompressed;
                logoImporter.spriteBorder=Vector4.zero;logoImporter.SaveAndReimport();
                logo.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(logoPath);
                if(logo.sprite==null)throw new Exception("Missing pencil category logo: "+logos[index]);
                logo.type=Image.Type.Simple;logo.preserveAspect=true;logo.raycastTarget=false;logo.color=Color.white;
            }
            var back=NewButton(lobby.categoryPanel.transform,"Category back","Geri",.5f,.075f,460,100);
            if(back.onClick.GetPersistentEventCount()==0)UnityEventTools.AddPersistentListener(back.onClick,lobby.BackToModeSelection);
            RectPanel(lobby.profilePanel,.08f,.24f,.92f,.73f);
            Label(lobby.profilePanel.transform,"Profile heading","Profilim",.5f,.89f,800,90,56);
            Place(lobby.currentNameText.rectTransform,.5f,.73f,780,75);lobby.currentNameText.fontSize=40;
            Place((RectTransform)lobby.nameInput.transform,.5f,.50f,750,125);
            lobby.nameInput.fontAsset=font;lobby.nameInput.customCaretColor=true;lobby.nameInput.caretColor=PaperThemeBuilder.Ink;lobby.nameInput.selectionColor=PaperThemeBuilder.Green;
            foreach(var text in lobby.nameInput.GetComponentsInChildren<TMP_Text>(true)){text.fontSize=42;text.enableAutoSizing=false;}
            var save=lobby.profilePanel.GetComponentsInChildren<Button>(true).First(b=>b.name=="SaveButton");
            Place((RectTransform)save.transform,.5f,.23f,500,120);save.GetComponentInChildren<TMP_Text>().text="Kaydet ve dön";
            var saveLabel=save.GetComponentInChildren<TMP_Text>();saveLabel.fontSize=44;saveLabel.enableAutoSizing=false;Stretch(saveLabel.rectTransform);
            foreach(var panel in new[]{lobby.loginPanel,lobby.waitingPanel})
            {
                RectPanel(panel,.08f,.30f,.92f,.69f);
                foreach(var text in panel.GetComponentsInChildren<TMP_Text>(true).Where(t=>t.GetComponentInParent<Button>()==null))
                {
                    Place(text.rectTransform,.5f,panel==lobby.loginPanel?.5f:.62f,800,180);
                    text.rectTransform.anchorMin=new Vector2(.05f,text.rectTransform.anchorMin.y);
                    text.rectTransform.anchorMax=new Vector2(.95f,text.rectTransform.anchorMax.y);
                    text.rectTransform.sizeDelta=new Vector2(0,180);
                    text.margin=Vector4.zero;text.alignment=TextAlignmentOptions.Center;
                    text.fontSize=48;text.enableAutoSizing=true;text.fontSizeMin=24;text.fontSizeMax=48;
                    text.raycastTarget=false;
                }
                foreach(var button in panel.GetComponentsInChildren<Button>(true))
                {
                    Place((RectTransform)button.transform,.5f,.23f,500,120);
                    var label=button.GetComponentInChildren<TMP_Text>();if(label!=null){label.fontSize=44;label.enableAutoSizing=false;Stretch(label.rectTransform);}
                }
            }
            lobby.loginPanel.SetActive(false);lobby.waitingPanel.SetActive(false);lobby.categoryPanel.SetActive(false);lobby.profilePanel.SetActive(false);lobby.modeSelectionPanel.SetActive(true);
            foreach(var button in canvas.GetComponentsInChildren<Button>(true))
            {
                var image=button.GetComponent<Image>();if(image!=null)image.pixelsPerUnitMultiplier=1;
            }
            ConfigureOpening(safe, lobby, coin);
            PaperInteractionBuilder.Apply(scene);
            string after=Bindings(canvas.gameObject);
            if(!before.Split('\n').All(line=>after.Contains(line)))throw new Exception("An existing menu button binding was lost.");
            Directory.CreateDirectory(Output);File.WriteAllText(Output+"/bindings.txt",after);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        }
        finally{if(!loaded)EditorSceneManager.CloseScene(scene,true);}
    }

    public static string Bindings(GameObject root)
    {
        return string.Join("\n",root.GetComponentsInChildren<Button>(true).SelectMany(b=>Enumerable.Range(0,b.onClick.GetPersistentEventCount()).Select(i=>b.onClick.GetPersistentTarget(i)?.GetType().Name+":"+b.onClick.GetPersistentMethodName(i))));
    }
    static void ConfigureOpening(Transform safe, LobbyManager lobby, CoinDisplay coin)
    {
        var shader=Shader.Find("Paper Menu/Ordered UI Stroke");
        if(shader==null)throw new Exception("Menu drawing shader is missing.");
        const string materialPath="Assets/Sprites/PaperTheme/MenuOrderedStroke.mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if(material==null){material=new Material(shader);AssetDatabase.CreateAsset(material,materialPath);}
        material.shader=shader;material.SetFloat("_Progress",1);EditorUtility.SetDirty(material);
        var intro=safe.GetComponent<PaperMenuIntro>();if(intro==null)intro=safe.gameObject.AddComponent<PaperMenuIntro>();
        intro.strokeMaterial=material;
        intro.pencilSprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/PencilStickman/drawing-pencil.png");
        intro.writingSounds=Enumerable.Range(1,3).Select(i=>AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Pencil/pencil-writing-"+i+".wav")).ToArray();
        intro.drawingOrder.Clear();
        var title=safe.GetComponentsInChildren<TMP_Text>(true).First(t=>t.text.Trim()=="ADAM ASMACA");
        intro.drawingOrder.Add(new PaperMenuIntro.DrawItem{graphic=title,seconds=.55f});
        var subtitle=lobby.modeSelectionPanel.GetComponentsInChildren<TMP_Text>(true).First(t=>t.text=="Bir defter dolusu kelime");
        intro.drawingOrder.Add(new PaperMenuIntro.DrawItem{graphic=subtitle,seconds=.15f});
        foreach(var image in lobby.modeSelectionPanel.transform.Find("Menu pencil character").GetComponentsInChildren<Image>())
            intro.drawingOrder.Add(new PaperMenuIntro.DrawItem{graphic=image,seconds=image.name=="head"?.30f:.10f,profile=AssetDatabase.LoadAssetAtPath<PencilStrokeProfile>("Assets/Sprites/PencilStickman/"+image.name+"-strokes.asset")});
        foreach(var labelText in new[]{"Tek Oyunculu","Çok Oyunculu","Profilim"})
        {
            var button=lobby.modeSelectionPanel.GetComponentsInChildren<Button>(true).First(b=>b.GetComponentInChildren<TMP_Text>().text==labelText);
            intro.drawingOrder.Add(new PaperMenuIntro.DrawItem{graphic=button.GetComponent<Image>(),seconds=.55f});
            intro.drawingOrder.Add(new PaperMenuIntro.DrawItem{graphic=button.GetComponentInChildren<TMP_Text>(),seconds=.35f});
        }
        intro.drawingOrder.Add(new PaperMenuIntro.DrawItem{graphic=lobby.modeSelectionPanel.transform.Find("Menu footer").GetComponent<TMP_Text>(),seconds=.10f});
        if(coin!=null && coin.coinText!=null)intro.drawingOrder.Add(new PaperMenuIntro.DrawItem{graphic=coin.coinText,seconds=.05f});
        AssetDatabase.SaveAssets();
    }
    static void Style(GameObject root)
    {
        foreach(var text in root.GetComponentsInChildren<TMP_Text>(true)){text.font=font;text.fontSharedMaterial=font.material;text.fontStyle=FontStyles.Normal;text.color=PaperThemeBuilder.Ink;}
        foreach(var image in root.GetComponentsInChildren<Image>(true))
        {
            if(image.name=="Pencil category logo" || (image.transform.parent!=null && image.transform.parent.name=="Menu pencil character"))continue;
            image.sprite=frame;image.type=Image.Type.Sliced;image.color=Color.white;image.pixelsPerUnitMultiplier=1;
        }
        foreach(var button in root.GetComponentsInChildren<Button>(true))
        {var colors=button.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(.84f,.84f,.84f);colors.pressedColor=new Color(.60f,.60f,.60f);colors.selectedColor=colors.highlightedColor;button.colors=colors;}
    }
    static Sprite CreateOutline()
    {
        const string path="Assets/Sprites/PaperTheme/pencil-outline.png";
        var texture=new Texture2D(128,128,TextureFormat.RGBA32,false);
        for(int y=0;y<128;y++)for(int x=0;x<128;x++)
        {
            float horizontal=5.5f+.55f*Mathf.Sin(x*.19f)+.22f*Mathf.Sin(x*.67f);
            float vertical=5.5f+.55f*Mathf.Sin(y*.17f)+.22f*Mathf.Sin(y*.71f);
            float d=Mathf.Min(Mathf.Abs(y-horizontal),Mathf.Abs(y-(127-horizontal)));
            d=Mathf.Min(d,Mathf.Min(Mathf.Abs(x-vertical),Mathf.Abs(x-(127-vertical))));
            bool span=x>=4 && x<=123 && y>=4 && y<=123;
            float alpha=span ? Mathf.Clamp01(1.55f-d)*(.52f+.30f*Mathf.PerlinNoise(x*.39f,y*.41f)) : 0;
            texture.SetPixel(x,y,new Color(.19f,.18f,.17f,alpha));
        }
        texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
        importer.spriteBorder=new Vector4(16,16,16,16);importer.spritePixelsPerUnit=100;
        importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
    static void Stretch(RectTransform rect){rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;}
    static void Place(RectTransform rect,float x,float y,float width,float height){rect.anchorMin=rect.anchorMax=new Vector2(x,y);rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=Vector2.zero;rect.sizeDelta=new Vector2(width,height);rect.localScale=Vector3.one;}
    static void RectPanel(GameObject panel,float left,float bottom,float right,float top){var rect=panel.GetComponent<RectTransform>();rect.anchorMin=new Vector2(left,bottom);rect.anchorMax=new Vector2(right,top);rect.offsetMin=rect.offsetMax=Vector2.zero;}
    static TMP_Text Label(Transform parent,string name,string content,float x,float y,float width,float height,float size)
    {
        var existing=parent.Find(name);var text=existing!=null ? existing.GetComponent<TMP_Text>() : new GameObject(name,typeof(RectTransform),typeof(TextMeshProUGUI)).GetComponent<TMP_Text>();
        text.transform.SetParent(parent,false);Place(text.rectTransform,x,y,width,height);text.text=content;text.font=font;text.fontSharedMaterial=font.material;text.fontSize=size;text.color=PaperThemeBuilder.Ink;text.alignment=TextAlignmentOptions.Center;text.raycastTarget=false;return text;
    }
    static Button NewButton(Transform parent,string name,string content,float x,float y,float width,float height)
    {
        var existing=parent.Find(name);var button=existing!=null ? existing.GetComponent<Button>() : new GameObject(name,typeof(RectTransform),typeof(Image),typeof(Button)).GetComponent<Button>();
        button.transform.SetParent(parent,false);Place((RectTransform)button.transform,x,y,width,height);
        var image=button.GetComponent<Image>();image.sprite=frame;image.type=Image.Type.Sliced;button.targetGraphic=image;
        Label(button.transform,"Label",content,.5f,.5f,width-30,height-12,44);return button;
    }
    static void CreateCharacter(Transform parent)
    {
        if(parent.Find("Menu pencil character")!=null)return;
        var hero=new GameObject("Menu pencil character",typeof(RectTransform));hero.transform.SetParent(parent,false);Place(hero.GetComponent<RectTransform>(),.5f,.61f,420,420);
        var model=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PencilRagdollBuilder.PrefabPath));
        try
        {
            foreach(var part in model.GetComponent<RagdollController>().partsOrder)
            {
                var source=part.spriteRenderer;var image=new GameObject(part.body.name,typeof(RectTransform),typeof(Image)).GetComponent<Image>();image.transform.SetParent(hero.transform,false);
                image.sprite=source.sprite;image.raycastTarget=false;
                var rect=image.rectTransform;rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f);
                Vector3 point=model.transform.InverseTransformPoint(source.transform.position);
                rect.anchoredPosition=((Vector2)point+new Vector2(0,1.05f))*170;
                rect.sizeDelta=source.sprite.bounds.size*source.transform.lossyScale.x*170;
                rect.localRotation=source.transform.rotation;
            }
        }
        finally{UnityEngine.Object.DestroyImmediate(model);}
    }
}

