using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Draws the existing menu artwork once at application startup.</summary>
public class PaperMenuIntro : MonoBehaviour
{
    [System.Serializable]
    public class DrawItem
    {
        public Graphic graphic;
        public PencilStrokeProfile profile;
        [Min(.01f)] public float seconds = .1f;
    }
    public List<DrawItem> drawingOrder = new List<DrawItem>();
    public Material strokeMaterial;
    public Sprite pencilSprite;
    public AudioClip[] writingSounds;
    [Min(0)] public float liftDuration = .01f;
    public float duration => TotalDuration();
    [Range(0, 1)] public float soundVolume = .10f;
    public bool IsDrawing { get; private set; }
    public float Progress { get; private set; }
    public bool PencilVisible => pencil != null && pencil.enabled;
    static bool playedThisSession;
    readonly List<State> states = new List<State>();
    CanvasGroup input;
    bool originalInteractable;
    Image pencil;
    AudioSource sound;
    Coroutine routine;
    Vector2Int screenSize;

    class State
    {
        public DrawItem item;
        public Material original, temporary;
        public bool enabled;
        public RectTransform mask;
        public RectTransform rect;
        public Transform parent;
        public int sibling;
        public Vector2 anchorMin, anchorMax, pivot, position, size;
        public Vector3 scale;
        public Quaternion rotation;
        public float width, height, left, right;
    }

    void Awake()
    {
        if (playedThisSession || strokeMaterial == null || pencilSprite == null) { Progress = 1; return; }
        playedThisSession = true;
        Prepare();
    }
    IEnumerator Start()
    {
        if (!IsDrawing) yield break;
        yield return null; // Lobby initialization and safe-area layout finish first.
        Canvas.ForceUpdateCanvases();
        routine = StartCoroutine(Draw());
    }
    void Prepare()
    {
        screenSize = new Vector2Int(Screen.width, Screen.height);
        input = GetComponent<CanvasGroup>();
        if (input == null) input = gameObject.AddComponent<CanvasGroup>();
        originalInteractable = input.interactable; input.interactable = false;
        IsDrawing = true; Progress = 0;
        foreach (var item in drawingOrder)
        {
            if (item.graphic == null) continue;
            var state = new State { item = item, original = item.graphic.material, enabled = item.graphic.enabled };
            item.graphic.enabled = false;
            states.Add(state);
        }
        var obj = new GameObject("Opening drawing pencil", typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(transform, false);
        pencil = obj.GetComponent<Image>(); pencil.sprite = pencilSprite; pencil.raycastTarget = false;
        pencil.rectTransform.sizeDelta = new Vector2(100, 100);
        pencil.rectTransform.pivot = pencilSprite.pivot / pencilSprite.rect.size;
        pencil.rectTransform.localRotation = Quaternion.Euler(0, 0, -25); pencil.enabled = false;
        sound = obj.AddComponent<AudioSource>(); sound.playOnAwake = false; sound.loop = true; sound.spatialBlend = 0;
    }
    IEnumerator Draw()
    {
        float total = TotalDuration();
        float elapsedTotal = 0;
        for (int index = 0; index < states.Count; index++)
        {
            var state = states[index];
            Vector3 previous = pencil.transform.position;
            Initialize(state);
            if (index > 0 && liftDuration > 0)
            {
                Vector3 next = pencil.transform.position;
                float travel = 0;
                while (travel < liftDuration)
                {
                    float p = travel / liftDuration;
                    pencil.transform.position = Vector3.Lerp(previous, next, Mathf.SmoothStep(0, 1, p)) + transform.up * (Mathf.Sin(p * Mathf.PI) * 12);
                    pencil.enabled = true;
                    yield return null; travel += Time.unscaledDeltaTime;
                }
                elapsedTotal += liftDuration;
            }
            float length = Mathf.Max(.01f, state.item.seconds);
            if (writingSounds != null && writingSounds.Length > 0)
            { sound.clip = writingSounds[index % writingSounds.Length]; sound.pitch = .985f + (index % 3) * .015f; sound.volume = 0; sound.Play(); }
            float elapsed = 0;
            while (elapsed < length)
            {
                float fraction = elapsed / length;
                Tick(state, fraction, out bool contact);
                float envelope = Mathf.Clamp01(Mathf.Min(elapsed, length - elapsed) / .015f);
                sound.volume = contact ? soundVolume * envelope : 0;
                Progress = Mathf.Clamp01((elapsedTotal + elapsed) / total);
                yield return null;
                elapsed += Time.unscaledDeltaTime;
            }
            Tick(state, 1, out _); Restore(state); sound.Stop();
            elapsedTotal += length;
        }
        routine = null; Complete();
    }
    float TotalDuration()
    {
        float total = 0; int count = 0;
        foreach (var item in drawingOrder) if (item.graphic != null) { total += Mathf.Max(.01f, item.seconds); count++; }
        return total + Mathf.Max(0, count - 1) * liftDuration;
    }
    void Initialize(State state)
    {
        var graphic = state.item.graphic;
        graphic.enabled = state.enabled;
        if (graphic is TMP_Text text)
        {
            text.ForceMeshUpdate();
            var rect = text.rectTransform; state.rect = rect; state.parent = rect.parent; state.sibling = rect.GetSiblingIndex();
            state.anchorMin = rect.anchorMin; state.anchorMax = rect.anchorMax; state.pivot = rect.pivot;
            state.position = rect.anchoredPosition; state.size = rect.sizeDelta; state.scale = rect.localScale; state.rotation = rect.localRotation;
            state.width = rect.rect.width; state.height = rect.rect.height;
            state.left = state.width; state.right = 0;
            foreach (var character in text.textInfo.characterInfo)
            {
                if (!character.isVisible) continue;
                state.left = Mathf.Min(state.left, character.bottomLeft.x - rect.rect.xMin);
                state.right = Mathf.Max(state.right, character.topRight.x - rect.rect.xMin);
            }
            if (state.right <= state.left) { state.left = 0; state.right = state.width; }
            var mask = new GameObject("Opening writing mask", typeof(RectTransform), typeof(RectMask2D)).GetComponent<RectTransform>();
            mask.SetParent(state.parent, false); mask.SetSiblingIndex(state.sibling);
            mask.anchorMin = mask.anchorMax = new Vector2(.5f, .5f); mask.pivot = new Vector2(0, .5f);
            mask.sizeDelta = new Vector2(state.width, state.height); mask.localScale = state.scale; mask.localRotation = state.rotation;
            // Pin the actual left edge, including labels stretched inside their buttons.
            mask.position = rect.TransformPoint(new Vector2(rect.rect.xMin, rect.rect.center.y));
            rect.SetParent(mask, false); rect.anchorMin = rect.anchorMax = Vector2.zero;
            rect.sizeDelta = new Vector2(state.width, state.height); rect.anchoredPosition = new Vector2(state.pivot.x * state.width, state.pivot.y * state.height);
            rect.localScale = Vector3.one; rect.localRotation = Quaternion.identity;
            state.mask = mask; mask.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 0);
        }
        else
        {
            state.temporary = new Material(strokeMaterial);
            state.temporary.SetFloat("_Progress", 0);
            state.temporary.SetFloat("_Mode", state.item.profile == null ? 0 : 1);
            if (state.item.profile != null) state.temporary.SetTexture("_StrokeMask", state.item.profile.orderMask);
            var rect = graphic.rectTransform.rect;
            state.temporary.SetVector("_DrawingRect", new Vector4(rect.xMin, rect.yMin, rect.width, rect.height));
            graphic.material = state.temporary;
        }
        Tick(state, 0, out _);
    }
    void Tick(State state, float fraction, out bool contact)
    {
        contact = fraction < 1;
        var graphic = state.item.graphic;
        if (graphic == null || !graphic.gameObject.activeInHierarchy) { pencil.enabled = false; contact = false; return; }
        Vector2 point;
        if (state.mask != null && graphic is TMP_Text text)
        {
            float edge = Mathf.Lerp(state.left, state.right, fraction);
            state.mask.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, edge);
            float x = text.rectTransform.rect.xMin + edge;
            float y = 0;
            for (int i = 0; i < text.textInfo.characterCount; i++)
            {
                var c = text.textInfo.characterInfo[i]; if (!c.isVisible) continue;
                y = Mathf.Lerp(c.bottomLeft.y, c.topRight.y, .5f + .35f * Mathf.Sin(fraction * 110));
                if (c.topRight.x >= x) { contact = x >= c.bottomLeft.x && fraction < 1; break; }
            }
            point = new Vector2(x, y);
        }
        else if (state.item.profile != null)
        {
            state.temporary.SetFloat("_Progress", fraction);
            var uv = state.item.profile.Evaluate(fraction, out contact, out float lift);
            var rect = graphic.rectTransform.rect; point = rect.min + Vector2.Scale(uv, rect.size) + Vector2.up * lift * rect.height;
        }
        else
        {
            state.temporary.SetFloat("_Progress", fraction);
            var rect = graphic.rectTransform.rect; rect = new Rect(rect.xMin + 5.5f, rect.yMin + 5.5f, rect.width - 11, rect.height - 11);
            float perimeter = 2 * (rect.width + rect.height), distance = fraction * perimeter;
            if (distance < rect.width) point = new Vector2(rect.xMin + distance, rect.yMax);
            else if ((distance -= rect.width) < rect.height) point = new Vector2(rect.xMax, rect.yMax - distance);
            else if ((distance -= rect.height) < rect.width) point = new Vector2(rect.xMax - distance, rect.yMin);
            else point = new Vector2(rect.xMin, rect.yMin + distance - rect.width);
        }
        pencil.transform.position = graphic.rectTransform.TransformPoint(point);
        pencil.transform.SetAsLastSibling(); pencil.enabled = fraction < 1;
    }
    void Restore(State state)
    {
        if (state.item.graphic != null)
        {
            state.item.graphic.enabled = state.enabled;
            if (state.temporary != null) state.item.graphic.material = state.original;
        }
        if (state.mask != null && state.rect != null)
        {
            state.rect.SetParent(state.parent, false); state.rect.SetSiblingIndex(state.sibling);
            state.rect.anchorMin = state.anchorMin; state.rect.anchorMax = state.anchorMax; state.rect.pivot = state.pivot;
            state.rect.anchoredPosition = state.position; state.rect.sizeDelta = state.size; state.rect.localScale = state.scale; state.rect.localRotation = state.rotation;
            Destroy(state.mask.gameObject); state.mask = null;
        }
        if (state.temporary != null) { Destroy(state.temporary); state.temporary = null; }
    }
    public void Complete()
    {
        if (routine != null) { StopCoroutine(routine); routine = null; }
        foreach (var state in states) Restore(state);
        states.Clear();
        if (input != null) input.interactable = originalInteractable;
        if (sound != null) sound.Stop();
        if (pencil != null) { pencil.enabled = false; Destroy(pencil.gameObject); pencil = null; }
        IsDrawing = false; Progress = 1;
    }
    [ContextMenu("Replay opening drawing (Play Mode)")]
    public void Replay()
    {
        if (!Application.isPlaying || strokeMaterial == null || pencilSprite == null) return;
        Complete(); Canvas.ForceUpdateCanvases(); Prepare(); routine = StartCoroutine(Draw());
    }
    void OnDisable() { if (IsDrawing) Complete(); }
    void Update() { if (IsDrawing && screenSize != new Vector2Int(Screen.width, Screen.height)) Complete(); }
    void OnApplicationPause(bool paused) { if (paused && IsDrawing) Complete(); }
}
