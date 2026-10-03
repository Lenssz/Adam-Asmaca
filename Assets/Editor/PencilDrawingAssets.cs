using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class PencilDrawingAssets
{
    const string Folder = PencilRagdollBuilder.Folder;
    public static Material StrokeMaterial { get; private set; }
    public static Sprite Pencil { get; private set; }
    public static AudioClip Scratch { get; private set; }
    public static AudioClip[] WritingVariations { get; private set; }
    public static PencilStrokeProfile[] Profiles { get; private set; }
    static readonly string[] Names = { "head", "torso", "left-arm", "right-arm", "left-leg", "right-leg" };

    // Coordinates trace the existing graphite artwork, in source pixels from the top left.
    static Vector2[] Points(params float[] xy)
    {
        var result = new Vector2[xy.Length / 2];
        for (int i = 0; i < result.Length; i++) result[i] = new Vector2(xy[i * 2], xy[i * 2 + 1]);
        return result;
    }

    static Vector2[] Smooth(Vector2[] controls)
    {
        var result = new Vector2[(controls.Length - 1) * 8 + 1];
        for (int i = 0; i < controls.Length - 1; i++)
        {
            Vector2 a = controls[Mathf.Max(0, i - 1)], b = controls[i], c = controls[i + 1], d = controls[Mathf.Min(controls.Length - 1, i + 2)];
            for (int k = 0; k < 8; k++)
            {
                float t = k / 8f;
                result[i * 8 + k] = 0.5f * ((2 * b) + (-a + c) * t + (2 * a - 5 * b + 4 * c - d) * t * t + (-a + 3 * b - 3 * c + d) * t * t * t);
            }
        }
        result[result.Length - 1] = controls[controls.Length - 1];
        return result;
    }

    public static void Build()
    {
        Shader shader = Shader.Find("Pencil Stickman/Ordered Stroke");
        if (shader == null) throw new Exception("Pencil stroke shader is missing.");
        string materialPath = Folder + "/PencilStroke.mat";
        StrokeMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (StrokeMaterial == null) { StrokeMaterial = new Material(shader); AssetDatabase.CreateAsset(StrokeMaterial, materialPath); }
        StrokeMaterial.shader = shader;
        StrokeMaterial.SetFloat("_DrawProgress", 1);
        EditorUtility.SetDirty(StrokeMaterial);

        Profiles = new PencilStrokeProfile[6];
        for (int i = 0; i < 6; i++) Profiles[i] = BakeProfile(i);
        ImportPencil();
        WritingVariations = NaturalPencilAudio.Build();
        Scratch = WritingVariations[0];
        AssetDatabase.SaveAssets();
    }

    static PencilStrokeProfile BakeProfile(int index)
    {
        var source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        source.LoadImage(File.ReadAllBytes(Folder + "/" + Names[index] + ".png"));
        int width = source.width, height = source.height;
        Vector2[][] paths;
        float[] starts, ends;
        if (index == 0)
        {
            paths = new[] {
                Points(143,39, 192,50, 231,77, 260,116, 273,155, 271,196, 256,236, 230,268, 191,288, 145,299, 100,292, 58,273, 30,247, 13,213, 8,170, 18,129, 40,91, 73,65, 105,47, 143,39),
                Points(90,179, 92,190, 94,204),
                Points(158,168, 159,180, 161,194),
                Points(58,268, 82,275, 115,272, 148,262, 179,247, 207,228, 231,211),
                Points(27,50, 49,34, 73,28, 97,31, 112,46, 125,68),
                Points(110,5, 101,12, 93,24, 89,36),
                Points(140,10, 128,18, 119,29, 112,42),
                Points(203,17, 185,14, 169,20, 156,32, 157,52)
            };
            starts = new[] { 0f, .52f, .61f, .70f, .85f, .89f, .93f, .97f };
            ends = new[] { .50f, .59f, .68f, .83f, .88f, .92f, .96f, 1f };
        }
        else
        {
            paths = new[] { index == 1 ? Points(18,10, 21,100, 20,220, 18,338)
                : index == 2 ? Points(14,12, 51,53, 99,122, 130,190, 150,256)
                : index == 3 ? Points(155,12, 109,58, 68,121, 39,183, 16,264)
                : index == 4 ? Points(100,12, 109,105, 108,225, 94,303, 85,338, 59,353, 13,369)
                : Points(22,13, 16,120, 19,232, 33,315, 43,341, 75,359, 112,371) };
            starts = new[] { 0f }; ends = new[] { 1f };
        }

        var strokes = new PencilStrokeProfile.Stroke[paths.Length];
        for (int s = 0; s < paths.Length; s++)
        {
            paths[s] = Smooth(paths[s]);
            float[] distances = new float[paths[s].Length];
            for (int j = 1; j < distances.Length; j++) distances[j] = distances[j - 1] + Vector2.Distance(paths[s][j - 1], paths[s][j]);
            float length = distances[distances.Length - 1];
            for (int j = 0; j < distances.Length; j++) distances[j] /= length;
            var uv = new Vector2[paths[s].Length];
            for (int j = 0; j < uv.Length; j++) uv[j] = new Vector2((paths[s][j].x + .5f) / width, 1 - (paths[s][j].y + .5f) / height);
            strokes[s] = new PencilStrokeProfile.Stroke { start = starts[s], end = ends[s], points = uv, distances = distances };
        }

        var order = new Color32[width * height];
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                Vector2 pixel = new Vector2(x, height - 1 - y);
                float closest = float.MaxValue, time = 1, circleDistance = float.MaxValue, circleTime = 1;
                for (int s = 0; s < paths.Length; s++)
                    for (int j = 1; j < paths[s].Length; j++)
                    {
                        Vector2 a = paths[s][j - 1], ab = paths[s][j] - a;
                        float fraction = Mathf.Clamp01(Vector2.Dot(pixel - a, ab) / Mathf.Max(.0001f, ab.sqrMagnitude));
                        float distance = (pixel - (a + ab * fraction)).sqrMagnitude;
                        if (index == 0 && s == 0 && distance < circleDistance)
                        {
                            circleDistance = distance;
                            circleTime = Mathf.Lerp(starts[0], ends[0], Mathf.Lerp(strokes[0].distances[j - 1], strokes[0].distances[j], fraction));
                        }
                        if (distance >= closest) continue;
                        closest = distance;
                        float pathFraction = Mathf.Lerp(strokes[s].distances[j - 1], strokes[s].distances[j], fraction);
                        time = Mathf.Lerp(starts[s], ends[s], pathFraction);
                    }
                // Shared graphite at hair/outline crossings belongs to the first stroke,
                // so the completed circle does not acquire holes while waiting for hair.
                if (index == 0 && pixel.y >= 35 && circleDistance <= 64) time = circleTime;
                int encoded = Mathf.Clamp(Mathf.RoundToInt(time * 65535), 0, 65535);
                order[y * width + x] = new Color32((byte)(encoded >> 8), (byte)(encoded & 255), 0, 255);
            }
        var mask = new Texture2D(width, height, TextureFormat.RGBA32, false, true);
        mask.SetPixels32(order); mask.Apply();
        string maskPath = Folder + "/" + Names[index] + "-order.png";
        File.WriteAllBytes(maskPath, mask.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(mask); UnityEngine.Object.DestroyImmediate(source);
        AssetDatabase.ImportAsset(maskPath, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(maskPath);
        importer.textureType = TextureImporterType.Default;
        importer.sRGBTexture = false; importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.filterMode = FilterMode.Point; importer.wrapMode = TextureWrapMode.Clamp;
        importer.SaveAndReimport();
        string profilePath = Folder + "/" + Names[index] + "-strokes.asset";
        var profile = AssetDatabase.LoadAssetAtPath<PencilStrokeProfile>(profilePath);
        if (profile == null) { profile = ScriptableObject.CreateInstance<PencilStrokeProfile>(); AssetDatabase.CreateAsset(profile, profilePath); }
        profile.orderMask = AssetDatabase.LoadAssetAtPath<Texture2D>(maskPath);
        profile.duration = index == 0 ? .9f : .5f;
        profile.strokes = strokes;
        EditorUtility.SetDirty(profile);
        return profile;
    }

    static void ImportPencil()
    {
        string path = Folder + "/drawing-pencil.png";
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 100;
        importer.maxTextureSize = 512;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.wrapMode = TextureWrapMode.Clamp;
        var image = new Texture2D(2, 2); image.LoadImage(File.ReadAllBytes(path));
        Color32[] pixels = image.GetPixels32();
        int minY = image.height, sumX = 0, count = 0;
        for (int y = 0; y < image.height && count == 0; y++)
            for (int x = 0; x < image.width; x++)
                if (pixels[y * image.width + x].a > 128) { minY = y; sumX += x; count++; }
        var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        settings.spriteAlignment = (int)SpriteAlignment.Custom;
        settings.spritePivot = new Vector2((sumX / (float)Mathf.Max(1, count) + .5f) / image.width, (minY + .5f) / image.height);
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
        Pencil = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        UnityEngine.Object.DestroyImmediate(image);
    }

}
