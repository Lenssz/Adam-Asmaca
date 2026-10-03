using UnityEngine;
using UnityEditor;
using System.IO;

public class StickmanGenerator : EditorWindow
{
    [MenuItem("Tools/Generate Stickman & Update Ragdoll")]
    public static void GenerateAndUpdate()
    {
        string dir = "Assets/Sprites/Stickman";
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        // Generate Textures
        Texture2D headTex = GenerateHead();
        Texture2D torsoTex = GenerateLine(32, 128, 6);
        Texture2D armTex = GenerateLine(128, 32, 6, true);
        Texture2D legTex = GenerateLine(32, 128, 6);

        // Save to PNG
        string headPath = dir + "/head.png";
        string torsoPath = dir + "/torso.png";
        string armPath = dir + "/arm.png";
        string legPath = dir + "/leg.png";

        File.WriteAllBytes(headPath, headTex.EncodeToPNG());
        File.WriteAllBytes(torsoPath, torsoTex.EncodeToPNG());
        File.WriteAllBytes(armPath, armTex.EncodeToPNG());
        File.WriteAllBytes(legPath, legTex.EncodeToPNG());

        AssetDatabase.Refresh();

        // Setup Texture Importer
        SetupSprite(headPath);
        SetupSprite(torsoPath);
        SetupSprite(armPath);
        SetupSprite(legPath);

        // Load Sprites
        Sprite headSprite = AssetDatabase.LoadAssetAtPath<Sprite>(headPath);
        Sprite torsoSprite = AssetDatabase.LoadAssetAtPath<Sprite>(torsoPath);
        Sprite armSprite = AssetDatabase.LoadAssetAtPath<Sprite>(armPath);
        Sprite legSprite = AssetDatabase.LoadAssetAtPath<Sprite>(legPath);

        // Load Prefab
        string prefabPath = "Assets/Prefabs/RagdollAdam.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab != null)
        {
            RagdollController controller = prefab.GetComponent<RagdollController>();
            if (controller != null && controller.partsOrder.Length >= 6)
            {
                // partsOrder: 0=Head, 1=Torso, 2=LeftArm, 3=RightArm, 4=LeftLeg, 5=RightLeg
                if (controller.partsOrder[0].spriteRenderer != null) controller.partsOrder[0].spriteRenderer.sprite = headSprite;
                if (controller.partsOrder[1].spriteRenderer != null) controller.partsOrder[1].spriteRenderer.sprite = torsoSprite;
                if (controller.partsOrder[2].spriteRenderer != null) controller.partsOrder[2].spriteRenderer.sprite = armSprite;
                if (controller.partsOrder[3].spriteRenderer != null) controller.partsOrder[3].spriteRenderer.sprite = armSprite;
                if (controller.partsOrder[4].spriteRenderer != null) controller.partsOrder[4].spriteRenderer.sprite = legSprite;
                if (controller.partsOrder[5].spriteRenderer != null) controller.partsOrder[5].spriteRenderer.sprite = legSprite;

                // Adjust color to black if they were previously colored
                foreach (var part in controller.partsOrder)
                {
                    if (part.spriteRenderer != null)
                    {
                        part.spriteRenderer.color = Color.white; // Ensure color is white so our black stickman shows properly
                    }
                }

                EditorUtility.SetDirty(prefab);
                PrefabUtility.SavePrefabAsset(prefab);
                Debug.Log("RagdollAdam prefabı başarıyla çubuk adam çizimleriyle güncellendi!");
            }
            else
            {
                Debug.LogError("RagdollController veya partsOrder bulunamadı/eksik!");
            }
        }
        else
        {
            Debug.LogError("RagdollAdam.prefab bulunamadı!");
        }
    }

    static void SetupSprite(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }
    }

    static Texture2D GenerateHead()
    {
        int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] colors = new Color[size * size];
        for (int i = 0; i < colors.Length; i++) colors[i] = Color.clear;

        // Draw Circle Outline
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = 45f;
        float thickness = 4f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                if (Mathf.Abs(dist - radius) < thickness)
                {
                    colors[y * size + x] = Color.black;
                }
            }
        }

        // Draw Eyes
        DrawRect(colors, size, 45, 80, 6, 6, Color.black);
        DrawRect(colors, size, 83, 80, 6, 6, Color.black);

        // Draw Smile
        for (int x = 40; x <= 88; x++)
        {
            int y = 45 + (int)((Mathf.Pow(x - 64, 2)) / 50f);
            if (y > 35 && y < size)
            {
                DrawRect(colors, size, x, y, 4, 4, Color.black);
            }
        }

        // Draw Hair (3 lines)
        for (int y = 105; y < 125; y++)
        {
            DrawRect(colors, size, 64, y, 3, 3, Color.black);
            DrawRect(colors, size, 50 + (y - 105) / 2, y, 3, 3, Color.black);
            DrawRect(colors, size, 78 - (y - 105) / 2, y, 3, 3, Color.black);
        }

        tex.SetPixels(colors);
        tex.Apply();
        return tex;
    }

    static Texture2D GenerateLine(int width, int height, int thickness, bool horizontal = false)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] colors = new Color[width * height];
        for (int i = 0; i < colors.Length; i++) colors[i] = Color.clear;

        if (horizontal)
        {
            for (int x = 10; x < width - 10; x++)
            {
                // Slight wave
                int yOffset = (int)(Mathf.Sin(x * 0.1f) * 2f);
                int centerY = height / 2 + yOffset;
                for (int ty = -thickness / 2; ty <= thickness / 2; ty++)
                {
                    if (centerY + ty >= 0 && centerY + ty < height)
                        colors[(centerY + ty) * width + x] = Color.black;
                }
            }
        }
        else
        {
            for (int y = 10; y < height - 10; y++)
            {
                // Slight wave
                int xOffset = (int)(Mathf.Sin(y * 0.1f) * 2f);
                int centerX = width / 2 + xOffset;
                for (int tx = -thickness / 2; tx <= thickness / 2; tx++)
                {
                    if (centerX + tx >= 0 && centerX + tx < width)
                        colors[y * width + (centerX + tx)] = Color.black;
                }
            }
        }

        tex.SetPixels(colors);
        tex.Apply();
        return tex;
    }

    static void DrawRect(Color[] colors, int size, int cx, int cy, int w, int h, Color col)
    {
        for (int y = cy - h / 2; y <= cy + h / 2; y++)
        {
            for (int x = cx - w / 2; x <= cx + w / 2; x++)
            {
                if (x >= 0 && x < size && y >= 0 && y < size)
                {
                    colors[y * size + x] = col;
                }
            }
        }
    }
}
