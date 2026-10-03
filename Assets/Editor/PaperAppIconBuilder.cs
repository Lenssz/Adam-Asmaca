using UnityEditor;
using UnityEngine;
using System.IO;

public static class PaperAppIconBuilder
{
    public const string IconPath = "Assets/Sprites/PaperTheme/app-icon-pencil.png";

    [MenuItem("Tools/Pencil Stickman/7 Apply paper app icon")]
    public static void Apply()
    {
        AssetDatabase.ImportAsset(IconPath, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(IconPath);
        importer.textureType = TextureImporterType.Default;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = false;
        importer.maxTextureSize = 2048;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.SaveAndReimport();
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
        if (texture == null || texture.width != texture.height)
            throw new System.Exception("Application icon must be a square texture.");
        PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Unknown, new[] { texture });
        AssetDatabase.SaveAssets();
    }

    public static void RunBatch()
    {
        try
        {
            Apply();
            var icons = PlayerSettings.GetIconsForTargetGroup(BuildTargetGroup.Unknown);
            if (icons.Length == 0 || AssetDatabase.GetAssetPath(icons[0]) != IconPath)
                throw new System.Exception("Default application icon was not assigned.");
            Directory.CreateDirectory(".utmp/icon-validation");
            File.WriteAllText(".utmp/icon-validation/results.txt", "PASS: square opaque source imported without compression; Unity default application icon assigned. Device/package builds not tested.\n");
            EditorApplication.Exit(0);
        }
        catch (System.Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
    }
}
