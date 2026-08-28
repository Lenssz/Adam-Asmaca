using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(RawImage))]
public class HangmanDrawer : MonoBehaviour
{
    [Header("Renkler")]
    public Color drawColor = new Color(0.9f, 0.9f, 0.9f, 1f);
    public Color bgColor = new Color(0f, 0f, 0f, 0f);
    public Color ropeColor = new Color(0.7f, 0.5f, 0.2f, 1f);

    [Header("Çizim Ayarları")]
    public int textureWidth = 400;
    public int textureHeight = 500;
    public int lineThickness = 3;
    public float drawSpeed = 2f;

    // Koordinatlar (Aynı kalıyor)
    private Vector2Int gallowsBase1 = new Vector2Int(30, 10);
    private Vector2Int gallowsBase2 = new Vector2Int(170, 10);
    private Vector2Int gallowsPole = new Vector2Int(80, 10);
    private Vector2Int gallowsTop = new Vector2Int(80, 470);
    private Vector2Int gallowsArm = new Vector2Int(250, 470);
    private Vector2Int gallowsRope = new Vector2Int(250, 400);
    private Vector2Int headCenter = new Vector2Int(250, 370);
    private int headRadius = 35;
    private Vector2Int bodyTop = new Vector2Int(250, 335);
    private Vector2Int bodyBot = new Vector2Int(250, 230);
    private Vector2Int leftArmTop = new Vector2Int(250, 310);
    private Vector2Int leftArmBot = new Vector2Int(200, 265);
    private Vector2Int rightArmTop = new Vector2Int(250, 310);
    private Vector2Int rightArmBot = new Vector2Int(300, 265);
    private Vector2Int leftLegTop = new Vector2Int(250, 230);
    private Vector2Int leftLegBot = new Vector2Int(210, 170);
    private Vector2Int rightLegTop = new Vector2Int(250, 230);
    private Vector2Int rightLegBot = new Vector2Int(290, 170);

    private Texture2D tex;
    private RawImage rawImage;
    private bool isDrawing = false;
    private Queue<int> pendingParts = new Queue<int>();

    void Awake()
    {
        rawImage = GetComponent<RawImage>();
        CreateTexture();
        DrawGallows();
    }

    void OnEnable()
    {
        // GÜVENLİ KONTROL: GameManager sadece varsa dinle
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnLetterGuessed += OnLetterGuessed;
            GameManager.Instance.OnWordChanged += ResetDrawing;
        }
    }

    void OnDisable()
    {
        // GÜVENLİ KONTROL
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnLetterGuessed -= OnLetterGuessed;
            GameManager.Instance.OnWordChanged -= ResetDrawing;
        }
    }

    // MULTIPLAYER İÇİN: Dışarıdan yanlış sayısını alıp sıraya ekler
    public void AddPartToQueue(int wrongCount)
    {
        pendingParts.Enqueue(wrongCount);
        if (!isDrawing)
        {
            StartCoroutine(ProcessDrawingQueue());
        }
    }

    // SINGLEPLAYER İÇİN: GameManager'dan gelen veriyi işler
    void OnLetterGuessed(char letter, bool correct)
    {
        if (!correct && GameManager.Instance != null)
        {
            AddPartToQueue(GameManager.Instance.WrongGuessCount);
        }
    }

    // --- ÇİZİM MANTIĞI ---
    IEnumerator ProcessDrawingQueue()
    {
        isDrawing = true;
        while (pendingParts.Count > 0)
        {
            int partToDraw = pendingParts.Dequeue();
            yield return StartCoroutine(DrawPartAnimated(partToDraw));
        }
        isDrawing = false;
    }

    IEnumerator DrawPartAnimated(int partIndex)
    {
        switch (partIndex)
        {
            case 1: yield return StartCoroutine(DrawCircleAnimated(headCenter, headRadius, drawColor)); break;
            case 2: yield return StartCoroutine(DrawLineAnimated(bodyTop, bodyBot, drawColor)); break;
            case 3: yield return StartCoroutine(DrawLineAnimated(leftArmTop, leftArmBot, drawColor)); break;
            case 4: yield return StartCoroutine(DrawLineAnimated(rightArmTop, rightArmBot, drawColor)); break;
            case 5: yield return StartCoroutine(DrawLineAnimated(leftLegTop, leftLegBot, drawColor)); break;
            case 6: yield return StartCoroutine(DrawLineAnimated(rightLegTop, rightLegBot, drawColor)); break;
        }
    }

    // Diğer yardımcı fonksiyonlar (DrawLineAnimated, DrawCircleAnimated vb.) aynı kalsın...
    // Mevcut kodundaki DrawLineAnimated, DrawCircleAnimated, GetLinePoints, DrawThickPixel, CreateTexture, ClearTexture, DrawGallows, ResetDrawing, ShakeEffect fonksiyonlarını SİLMEYİP buraya altına ekle.

    // (Aşağısı senin orijinal yardımcı fonksiyonlarınla devam edecek)
    IEnumerator DrawLineAnimated(Vector2Int from, Vector2Int to, Color color)
    {
        List<Vector2Int> points = GetLinePoints(from, to);
        int totalPoints = points.Count;

        if (totalPoints == 0) yield break;

        float elapsedTime = 0f;
        int lastDrawnIndex = -1;

        // Gerçek zamana (drawSpeed saniyesine) göre çizimi tamamla
        while (elapsedTime < drawSpeed)
        {
            elapsedTime += Time.deltaTime; // Geçen süreyi cihazın FPS'ine göre topla

            // Geçen süreye oranla çizginin yüzde kaçındayız?
            float progress = elapsedTime / drawSpeed;
            int currentIndex = Mathf.Clamp(Mathf.FloorToInt(progress * totalPoints), 0, totalPoints - 1);

            // Eğer yeni noktalara ulaştıysak, aradaki tüm noktaları çiz
            if (currentIndex > lastDrawnIndex)
            {
                for (int i = lastDrawnIndex + 1; i <= currentIndex; i++)
                {
                    DrawThickPixel(points[i].x, points[i].y, color);
                }
                tex.Apply();
                lastDrawnIndex = currentIndex;
            }

            yield return null; // Bir sonraki kareyi bekle (FPS'ten bağımsız!)
        }

        // Güvenlik için: Süre bitse bile çizilmeyen son noktaları tamamla
        for (int i = lastDrawnIndex + 1; i < totalPoints; i++)
        {
            DrawThickPixel(points[i].x, points[i].y, color);
        }
        tex.Apply();
    }

    IEnumerator DrawCircleAnimated(Vector2Int center, int radius, Color color)
    {
        int totalSteps = 80;
        float elapsedTime = 0f;
        int lastDrawnStep = -1;

        // Kafa çizimi vücuda göre daha kısa sürsün (İstersen buradaki 0.5f çarpanını değiştirebilirsin)
        float circleDrawTime = drawSpeed * 0.5f;

        while (elapsedTime < circleDrawTime)
        {
            elapsedTime += Time.deltaTime;

            float progress = elapsedTime / circleDrawTime;
            int currentStep = Mathf.Clamp(Mathf.FloorToInt(progress * totalSteps), 0, totalSteps);

            if (currentStep > lastDrawnStep)
            {
                for (int i = lastDrawnStep + 1; i <= currentStep; i++)
                {
                    float angle = Mathf.Lerp(90f, 90f + 360f, (float)i / totalSteps) * Mathf.Deg2Rad;
                    int x = center.x + Mathf.RoundToInt(Mathf.Cos(angle) * radius);
                    int y = center.y + Mathf.RoundToInt(Mathf.Sin(angle) * radius);
                    DrawThickPixel(x, y, color);
                }
                tex.Apply();
                lastDrawnStep = currentStep;
            }

            yield return null;
        }

        // Son adımı garantiye al
        for (int i = lastDrawnStep + 1; i <= totalSteps; i++)
        {
            float angle = Mathf.Lerp(90f, 90f + 360f, (float)i / totalSteps) * Mathf.Deg2Rad;
            int x = center.x + Mathf.RoundToInt(Mathf.Cos(angle) * radius);
            int y = center.y + Mathf.RoundToInt(Mathf.Sin(angle) * radius);
            DrawThickPixel(x, y, color);
        }
        tex.Apply();
    }
    void DrawLineInstant(Vector2Int from, Vector2Int to, Color color) { List<Vector2Int> points = GetLinePoints(from, to); foreach (Vector2Int p in points) DrawThickPixel(p.x, p.y, color); }
    List<Vector2Int> GetLinePoints(Vector2Int from, Vector2Int to) { List<Vector2Int> points = new List<Vector2Int>(); int x0 = from.x, y0 = from.y; int x1 = to.x, y1 = to.y; int dx = Mathf.Abs(x1 - x0), dy = Mathf.Abs(y1 - y0); int sx = x0 < x1 ? 1 : -1; int sy = y0 < y1 ? 1 : -1; int err = dx - dy; while (true) { points.Add(new Vector2Int(x0, y0)); if (x0 == x1 && y0 == y1) break; int e2 = 2 * err; if (e2 > -dy) { err -= dy; x0 += sx; } if (e2 < dx) { err += dx; y0 += sy; } } return points; }
    void DrawThickPixel(int cx, int cy, Color color) { int half = lineThickness / 2; for (int ox = -half; ox <= half; ox++) for (int oy = -half; oy <= half; oy++) { int x = cx + ox; int y = cy + oy; if (x >= 0 && x < textureWidth && y >= 0 && y < textureHeight) tex.SetPixel(x, y, color); } }
    void CreateTexture() { tex = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false); tex.filterMode = FilterMode.Bilinear; ClearTexture(); rawImage.texture = tex; }
    void ClearTexture() { Color[] pixels = new Color[textureWidth * textureHeight]; for (int i = 0; i < pixels.Length; i++) pixels[i] = bgColor; tex.SetPixels(pixels); tex.Apply(); }
    void DrawGallows() { DrawLineInstant(gallowsBase1, gallowsBase2, drawColor); DrawLineInstant(gallowsPole, gallowsTop, drawColor); DrawLineInstant(gallowsTop, gallowsArm, drawColor); DrawLineInstant(gallowsArm, gallowsRope, ropeColor); tex.Apply(); }
    public void ResetDrawing() { StopAllCoroutines(); pendingParts.Clear(); isDrawing = false; ClearTexture(); DrawGallows(); }
    public void PlayLoseEffect() { StartCoroutine(ShakeEffect()); }
    IEnumerator ShakeEffect() { Vector3 original = transform.localPosition; for (int i = 0; i < 12; i++) { transform.localPosition = original + new Vector3(Random.Range(-6f, 6f), Random.Range(-4f, 4f), 0); yield return new WaitForSeconds(0.05f); } transform.localPosition = original; }
}