using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class PaperPressFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler, ISubmitHandler
{
    public float pressDuration = .08f, releaseDuration = .18f;
    [Range(0, 1)] public float creaseStrength = 1;
    [Range(0, .05f)] public float compression = .015f;
    public float Amount { get; private set; }
    public Vector2 ContactPoint { get; private set; }
    Button button;
    Vector3 restScale;
    PaperCreaseGraphic crease;
    bool held;
    int pointer;
    float submitUntil;

    void Awake() { button = GetComponent<Button>(); restScale = transform.localScale; }
    public void OnPointerDown(PointerEventData data)
    {
        if (!button.IsActive() || !button.IsInteractable() || PaperPageTransition.IsTransitioning || held) return;
        RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, data.position, data.pressEventCamera, out Vector2 point);
        Begin(point); pointer = data.pointerId;
    }
    void Begin(Vector2 point)
    {
        ContactPoint = point; held = true;
        if (crease == null)
        {
            var go = new GameObject("Finger paper creases", typeof(RectTransform), typeof(CanvasRenderer), typeof(PaperCreaseGraphic));
            go.transform.SetParent(transform, false); crease = go.GetComponent<PaperCreaseGraphic>();
            crease.raycastTarget = false;
            var rect = crease.rectTransform; rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f); rect.pivot = new Vector2(.5f, .5f);
        }
        float radius = Mathf.Clamp(Mathf.Min(((RectTransform)transform).rect.width, ((RectTransform)transform).rect.height) * .45f, 35, 90);
        crease.rectTransform.anchoredPosition = point - ((RectTransform)transform).rect.center;
        crease.rectTransform.sizeDelta = Vector2.one * radius * 2;
        crease.gameObject.SetActive(true); crease.transform.SetAsLastSibling();
    }
    public void OnPointerUp(PointerEventData data) { if (data.pointerId == pointer) held = false; }
    public void OnPointerExit(PointerEventData data)
    {
        if (!held || data.pointerId != pointer) return;
        held = false; data.eligibleForClick = false;
    }
    public void OnSubmit(BaseEventData data)
    {
        if (!button.IsActive() || !button.IsInteractable() || PaperPageTransition.IsTransitioning) return;
        Begin(((RectTransform)transform).rect.center); submitUntil = Time.unscaledTime + pressDuration;
    }
    void Update()
    {
        if (submitUntil > 0 && Time.unscaledTime >= submitUntil) { held = false; submitUntil = 0; }
        if (!button.IsInteractable()) held = false;
        Amount = Mathf.MoveTowards(Amount, held ? 1 : 0, Time.unscaledDeltaTime / Mathf.Max(.01f, held ? pressDuration : releaseDuration));
        transform.localScale = restScale * (1 - compression * Mathf.SmoothStep(0, 1, Amount));
        if (crease != null) { crease.intensity = Amount * creaseStrength; crease.SetVerticesDirty(); if (Amount == 0) crease.gameObject.SetActive(false); }
    }
    void OnDisable() { held = false; submitUntil = 0; Amount = 0; transform.localScale = restScale; if (crease != null) crease.gameObject.SetActive(false); }
}

// Native UI mesh: faint shading and paired highlights bend the paper beneath the finger.
[RequireComponent(typeof(CanvasRenderer))]
public class PaperCreaseGraphic : MaskableGraphic
{
    public float intensity;
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear(); float radius = rectTransform.rect.width * .5f;
        const int spokes = 24;
        vh.AddVert(Vector3.zero, new Color(.28f, .25f, .20f, .065f * intensity), Vector2.zero);
        for (int i = 0; i < spokes; i++)
        {
            float angle = i * Mathf.PI * 2 / spokes;
            vh.AddVert(new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)) * radius, new Color(.28f, .25f, .20f, 0), Vector2.zero);
        }
        for (int i = 0; i < spokes; i++) vh.AddTriangle(0, i + 1, (i + 1) % spokes + 1);
        for (int i = 0; i < 5; i++)
        {
            float angle = i * 1.23f + .32f;
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            Vector2 side = new Vector2(-direction.y, direction.x);
            Vector2 start = direction * radius * .12f, end = direction * radius * (.60f + (i % 3) * .12f);
            Strip(vh, start, end, side * 1.2f, new Color(.28f, .25f, .20f, .14f * intensity));
            Strip(vh, start + side * 2, end + side * 2, side * 1.1f, new Color(1, 1, .98f, .24f * intensity));
        }
    }
    static void Strip(VertexHelper vh, Vector2 a, Vector2 b, Vector2 thickness, Color color)
    {
        int index = vh.currentVertCount; Color end = color; end.a = 0;
        vh.AddVert(a - thickness, color, Vector2.zero); vh.AddVert(a + thickness, color, Vector2.zero);
        vh.AddVert(b + thickness * .15f, end, Vector2.zero); vh.AddVert(b - thickness * .15f, end, Vector2.zero);
        vh.AddTriangle(index, index + 1, index + 2); vh.AddTriangle(index, index + 2, index + 3);
    }
}
