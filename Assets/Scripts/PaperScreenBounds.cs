using System;
using UnityEngine;

[DefaultExecutionOrder(-200)]
public class PaperScreenBounds : MonoBehaviour
{
    public Camera sceneCamera;
    public PhysicsMaterial2D wallMaterial;
    public float edgeInset = 0.045f;
    public event Action BoundsChanged;
    public Rect InnerRect { get; private set; }
    public Rect FullRect { get; private set; }
    public Vector2 ReferenceCenter { get; private set; }
    readonly BoxCollider2D[] walls = new BoxCollider2D[4];
    int lastWidth, lastHeight;
    float lastAspect;
    Rect lastSafeArea;
    bool initialized;

    void Awake() => RefreshNow();
    void Update()
    {
        if (sceneCamera != null && (Screen.width != lastWidth || Screen.height != lastHeight ||
            Screen.safeArea != lastSafeArea || !Mathf.Approximately(sceneCamera.aspect, lastAspect))) RefreshNow();
    }

    void Initialize()
    {
        if (initialized) return;
        if (sceneCamera == null) sceneCamera = Camera.main;
        if (sceneCamera == null) return;
        initialized = true;
        ReferenceCenter = sceneCamera.transform.position;
        var rigidbody = gameObject.GetComponent<Rigidbody2D>();
        if (rigidbody == null) rigidbody = gameObject.AddComponent<Rigidbody2D>();
        rigidbody.bodyType = RigidbodyType2D.Static;
        gameObject.layer = 2; // Ignore Raycast: walls collide but never intercept grabbing.
        for (int i = 0; i < walls.Length; i++)
        {
            string name = new[] { "Left edge", "Right edge", "Bottom edge", "Top edge" }[i];
            Transform existing = transform.Find(name);
            var child = existing != null ? existing.gameObject : new GameObject(name);
            child.transform.SetParent(transform, false); child.layer = 2;
            walls[i] = child.GetComponent<BoxCollider2D>();
            if (walls[i] == null) walls[i] = child.AddComponent<BoxCollider2D>();
            walls[i].sharedMaterial = wallMaterial;
        }
    }

    public void RefreshNow()
    {
        Initialize();
        if (!initialized) return;
        lastWidth = Mathf.Max(1, Screen.width); lastHeight = Mathf.Max(1, Screen.height);
        lastAspect = sceneCamera.aspect; lastSafeArea = Screen.safeArea;
        Rect safe = new Rect(lastSafeArea.x / lastWidth, lastSafeArea.y / lastHeight, lastSafeArea.width / lastWidth, lastSafeArea.height / lastHeight);
        RefreshForViewport(safe, lastAspect);
    }

    // Normalized safe area makes editor render targets and device aspect tests deterministic.
    public void RefreshForViewport(Rect safeArea01, float aspect)
    {
        Initialize();
        if (!initialized) return;
        float height = sceneCamera.orthographicSize * 2;
        FullRect = new Rect(ReferenceCenter.x - height * aspect / 2, ReferenceCenter.y - height / 2, height * aspect, height);
        float left = Mathf.Clamp01(safeArea01.xMin), right = Mathf.Clamp01(safeArea01.xMax);
        float bottom = Mathf.Clamp01(safeArea01.yMin), top = Mathf.Clamp01(safeArea01.yMax);
        InnerRect = Rect.MinMaxRect(FullRect.xMin + FullRect.width * left + edgeInset,
            FullRect.yMin + FullRect.height * bottom + edgeInset,
            FullRect.xMin + FullRect.width * right - edgeInset,
            FullRect.yMin + FullRect.height * top - edgeInset);
        const float thickness = 0.5f;
        Vector2[] positions = {
            new Vector2(InnerRect.xMin - thickness / 2, InnerRect.center.y),
            new Vector2(InnerRect.xMax + thickness / 2, InnerRect.center.y),
            new Vector2(InnerRect.center.x, InnerRect.yMin - thickness / 2),
            new Vector2(InnerRect.center.x, InnerRect.yMax + thickness / 2) };
        for (int i = 0; i < walls.Length; i++)
        {
            walls[i].transform.position = positions[i];
            walls[i].size = i < 2 ? new Vector2(thickness, InnerRect.height + thickness * 2) : new Vector2(InnerRect.width + thickness * 2, thickness);
        }
        Physics2D.SyncTransforms();
        BoundsChanged?.Invoke();
    }

    public Vector2 ClampTarget(Rigidbody2D body, Vector2 localGrab, Vector2 target, SpriteRenderer artwork)
    {
        Vector2 grab = body.GetRelativePoint(localGrab);
        Bounds bounds = artwork != null ? artwork.bounds : body.GetComponent<Collider2D>().bounds;
        Vector2 delta = target - grab;
        delta.x = Mathf.Clamp(delta.x, InnerRect.xMin - bounds.min.x, InnerRect.xMax - bounds.max.x);
        delta.y = Mathf.Clamp(delta.y, InnerRect.yMin - bounds.min.y, InnerRect.yMax - bounds.max.y);
        return grab + delta;
    }
}
