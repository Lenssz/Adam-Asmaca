using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public static class PaperInteractionBuilder
{
    const string Art="Assets/Sprites/PaperTheme/";
    [MenuItem("Tools/Pencil Stickman/6 Apply paper interactions")]
    public static void Build()
    {
        foreach(var path in new[]{"Assets/Scenes/SampleScene.unity","Assets/Scenes/GAMESCENE.unity","Assets/Scenes/MultiplayerGameScene.unity"})
        {
            var scene=SceneManager.GetSceneByPath(path);bool loaded=scene.IsValid() && scene.isLoaded;
            if(!loaded)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            try { if(scene.isDirty)throw new System.Exception("Save scene changes before building paper interactions: "+path);Apply(scene);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene); }
            finally { if(!loaded)EditorSceneManager.CloseScene(scene,true); }
        }
        const string key="Assets/Prefabs/PaperTheme/PaperKeyButton.prefab";
        var prefab=PrefabUtility.LoadPrefabContents(key);
        try { if(prefab.GetComponent<PaperPressFeedback>()==null)prefab.AddComponent<PaperPressFeedback>();PrefabUtility.SaveAsPrefabAsset(prefab,key); }
        finally { PrefabUtility.UnloadPrefabContents(prefab); }
    }
    public static void Apply(Scene scene)
    {
        var root=scene.GetRootGameObjects().FirstOrDefault(g=>g.name=="Paper interaction service");
        if(root==null){root=new GameObject("Paper interaction service");SceneManager.MoveGameObjectToScene(root,scene);}
        var service=root.GetComponent<PaperPageTransition>();if(service==null)service=root.AddComponent<PaperPageTransition>();
        service.eraseMaterial=Material("PaperErase.mat","Paper Menu/Erase Page");
        service.entryMaterial=Material("MenuOrderedStroke.mat","Paper Menu/Ordered UI Stroke");
        service.paperTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"notebook-paper.png");
        service.handwriting=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Paper/PatrickHand SDF.asset");
        foreach(var button in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Button>(true)))
            if(button.GetComponent<PaperPressFeedback>()==null)button.gameObject.AddComponent<PaperPressFeedback>();
        AssetDatabase.SaveAssets();
    }
    static Material Material(string file,string shaderName)
    {
        var shader=Shader.Find(shaderName);if(shader==null)throw new System.Exception("Missing paper shader: "+shaderName);
        var asset=AssetDatabase.LoadAssetAtPath<Material>(Art+file);
        if(asset==null){asset=new Material(shader);AssetDatabase.CreateAsset(asset,Art+file);}asset.shader=shader;EditorUtility.SetDirty(asset);return asset;
    }
}
