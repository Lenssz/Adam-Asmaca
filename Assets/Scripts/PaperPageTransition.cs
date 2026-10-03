using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DefaultExecutionOrder(-250)]
public class PaperPageTransition : MonoBehaviour
{
    public Material eraseMaterial, entryMaterial;
    public Texture paperTexture;
    public TMP_FontAsset handwriting;
    public float eraseDuration = .45f, categoryEntryDuration = .7f, gameEntryDuration = .35f;
    public static PaperPageTransition Instance { get; private set; }
    public static bool IsTransitioning => Instance != null && Instance.busy;
    public bool WaitingForNetworkScene => busy && networkScene && !sceneArrived;
    public float Progress { get; private set; }
    public string Phase { get; private set; } = "Idle";
#if UNITY_EDITOR
    // Lets integration checks hold real asynchronous loading at its activation boundary.
    public Func<bool> ValidationLoadingReady;
#endif
    bool busy, skipAnimation, sceneArrived, networkScene, panelApplied;
    int destinationVersion;
    Vector2Int screenSize;
    Vector2Int viewportSize;
    Camera sourceCamera;
    Action destination;
    float incomingDuration;
    AsyncOperation loading;
    Coroutine routine;
    Canvas overlay;
    RawImage surface;
    PaperEraserGraphic eraser;
    TMP_Text loadingLabel;
    Material eraseInstance;
    RenderTexture snapshot;
    readonly List<EntryState> entryStates = new List<EntryState>();
    readonly List<InputState> inputStates = new List<InputState>();
    class InputState { public CanvasGroup group; public bool interactable; }
    class EntryState
    {
        public Graphic graphic;
        public Material original, temporary;
        public TMP_Text text;
        public int visible;
        public SpriteRenderer sprite;
        public Color color;
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this; DontDestroyOnLoad(gameObject); SceneManager.sceneLoaded += SceneLoaded;
    }
    public static bool ShowPanel(Action change, float seconds = .7f, bool authoritative = false)
    {
        if (Instance == null) { change(); return true; }
        var owner = Instance;
        if (owner.busy)
        {
            if (!authoritative || owner.loading != null || owner.networkScene) return false;
            owner.destination = change; owner.incomingDuration = seconds; owner.destinationVersion++; return true;
        }
        owner.destination = change; owner.incomingDuration = seconds; owner.Begin(); return true;
    }
    public static bool LoadScene(string name)
    {
        if (IsTransitioning) return false;
        if (Instance == null) { SceneManager.LoadSceneAsync(name); return true; }
        if (!Application.CanStreamedLevelBeLoaded(name)) { Debug.LogError("Paper page cannot load scene: " + name); return false; }
        var owner = Instance;
        owner.incomingDuration = name == "SampleScene" ? owner.categoryEntryDuration : owner.gameEntryDuration;
        owner.Prepare();
        try { owner.loading = SceneManager.LoadSceneAsync(name); owner.loading.allowSceneActivation = false; }
        catch (Exception error) { Debug.LogException(error); owner.Finish(); return false; }
        owner.routine = owner.StartCoroutine(owner.Guarded(owner.Run())); return true;
    }
    public static void CoverNetworkLoad(Action startLoading)
    {
        if (Instance == null) { startLoading(); return; }
        var owner = Instance;
        if (owner.busy)
        {
            // A room event supersedes a pending panel transition without delaying the network call.
            if (owner.networkScene) return;
            owner.CancelImmediate();
        }
        owner.networkScene = true; owner.incomingDuration = owner.gameEntryDuration; owner.Prepare();
        owner.routine = owner.StartCoroutine(owner.Guarded(owner.Run()));
        startLoading();
    }
    void Begin() { Prepare(); routine = StartCoroutine(Guarded(Run())); }
    void Prepare()
    {
        busy = true; skipAnimation = false; sceneArrived = false; panelApplied = false; Progress = 0;
        screenSize = new Vector2Int(Screen.width, Screen.height);
        if (overlay == null) CreateOverlay();
        BindCamera();
        overlay.enabled = false;
        var camera = Camera.main;
        sourceCamera = camera;
        if (camera != null) viewportSize = new Vector2Int(camera.pixelWidth, camera.pixelHeight);
        if (camera != null)
        {
            var target = camera.targetTexture;
            int width = target != null ? target.width : Screen.width;
            int height = target != null ? target.height : Screen.height;
            float limit = Mathf.Min(1, 1560f / Mathf.Max(1, Mathf.Max(width, height)));
            width = Mathf.Max(1, Mathf.RoundToInt(width * limit)); height = Mathf.Max(1, Mathf.RoundToInt(height * limit));
            if (snapshot == null || snapshot.width != width || snapshot.height != height)
            { ReleaseSnapshot(); snapshot = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32) { name = "Paper page snapshot" }; snapshot.Create(); }
            try { camera.targetTexture = snapshot; Canvas.ForceUpdateCanvases(); camera.Render(); }
            finally { camera.targetTexture = target; Canvas.ForceUpdateCanvases(); }
        }
        surface.texture = snapshot; surface.enabled = true; surface.color = Color.white;
        LockInputs();
        eraseInstance.SetTexture("_PaperTex", paperTexture); eraseInstance.SetFloat("_Progress", 0);
        loadingLabel.enabled = false; eraser.enabled = true; overlay.enabled = true; Canvas.ForceUpdateCanvases(); eraser.SetAllDirty();
    }
    void CreateOverlay()
    {
        var go = new GameObject("Paper page transition overlay", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        go.transform.SetParent(transform, false); overlay = go.GetComponent<Canvas>(); overlay.renderMode = RenderMode.ScreenSpaceCamera; overlay.sortingOrder = 32760;
        var scaler = go.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1080, 1920); scaler.matchWidthOrHeight = .5f;
        surface = new GameObject("Erased page", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>(); surface.transform.SetParent(go.transform, false); Stretch(surface.rectTransform);
        eraseInstance = new Material(eraseMaterial); surface.material = eraseInstance; surface.raycastTarget = true;
        var blocker = new GameObject("Page input blocker", typeof(RectTransform), typeof(Image)).GetComponent<Image>(); blocker.transform.SetParent(go.transform, false); Stretch(blocker.rectTransform); blocker.color = Color.clear; blocker.raycastTarget = true;
        eraser = new GameObject("Graphite eraser", typeof(RectTransform), typeof(CanvasRenderer), typeof(PaperEraserGraphic)).GetComponent<PaperEraserGraphic>(); eraser.transform.SetParent(go.transform, false); eraser.rectTransform.sizeDelta = new Vector2(84, 38); eraser.rectTransform.localRotation = Quaternion.Euler(0, 0, -12); eraser.raycastTarget = false;
        eraser.rectTransform.anchorMin = eraser.rectTransform.anchorMax = new Vector2(.5f,.5f);
        loadingLabel = new GameObject("Paper loading note", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TMP_Text>(); loadingLabel.transform.SetParent(go.transform, false);
        loadingLabel.rectTransform.sizeDelta = new Vector2(600, 90); loadingLabel.font = handwriting; loadingLabel.fontSize = 36; loadingLabel.text = "Hazırlanıyor…"; loadingLabel.alignment = TextAlignmentOptions.Center; loadingLabel.color = new Color(.19f, .18f, .17f); loadingLabel.raycastTarget = false;
        loadingLabel.rectTransform.anchorMin = loadingLabel.rectTransform.anchorMax = new Vector2(.5f,.5f);
    }
    void BindCamera()
    {
        if (overlay == null) return;
        overlay.worldCamera = Camera.main;
        if (Camera.main != null) overlay.planeDistance = Camera.main.nearClipPlane + .1f;
    }
    void LockInputs()
    {
        if (UnityEngine.EventSystems.EventSystem.current != null) UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
        foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (!canvas.isRootCanvas || canvas.transform.IsChildOf(transform)) continue;
            var group = canvas.GetComponent<CanvasGroup>(); if (group == null) group = canvas.gameObject.AddComponent<CanvasGroup>();
            if (inputStates.Any(state => state.group == group)) continue;
            inputStates.Add(new InputState { group = group, interactable = group.interactable }); group.interactable = false;
        }
    }
    IEnumerator Guarded(IEnumerator work)
    {
        while (true)
        {
            bool next;
            try { next = work.MoveNext(); }
            catch (Exception error) { Debug.LogException(error); if (loading != null) loading.allowSceneActivation = true; Finish(); yield break; }
            if (!next) yield break;
            yield return work.Current;
        }
    }
    IEnumerator Run()
    {
        Phase = "Erasing"; float elapsed = 0;
        while (elapsed < eraseDuration && !skipAnimation)
        {
            Progress = elapsed / Mathf.Max(.01f, eraseDuration); eraseInstance.SetFloat("_Progress", Progress);
            var rect = ((RectTransform)overlay.transform).rect;
            float y = 1 - Progress;
            eraser.rectTransform.anchoredPosition = new Vector2(Mathf.Lerp(rect.xMin, rect.xMax, .5f + .43f * Mathf.Sin(Progress * Mathf.PI * 10)), Mathf.Lerp(rect.yMin, rect.yMax, y));
            yield return null; elapsed += Time.unscaledDeltaTime;
        }
        eraseInstance.SetFloat("_Progress", 1); eraser.enabled = false; Phase = "Loading";
        float waited = 0;
        if (loading != null)
        {
            while (!ReadyForActivation()) { waited += Time.unscaledDeltaTime; loadingLabel.enabled = waited > .25f; yield return null; }
            loading.allowSceneActivation = true;
            while (!loading.isDone) yield return null;
        }
        else if (networkScene)
        {
            while (!sceneArrived) { waited += Time.unscaledDeltaTime; loadingLabel.enabled = waited > .25f; yield return null; }
        }
        else { destination?.Invoke(); panelApplied = true; }
        yield return null; // The new scene's Start methods and dynamic keyboard have run.
        BindCamera(); loadingLabel.enabled = false;
        int version;
        do
        {
            version = destinationVersion;
            if (panelApplied && destination != null && Phase == "Entering") destination();
            LockInputs(); PrepareEntry(); surface.enabled = false; Phase = "Entering"; elapsed = 0;
            while (elapsed < incomingDuration && !skipAnimation && version == destinationVersion)
            {
                Progress = elapsed / Mathf.Max(.01f, incomingDuration); TickEntry(Progress);
                yield return null; elapsed += Time.unscaledDeltaTime;
            }
            RestoreEntry();
        } while (version != destinationVersion && !skipAnimation);
        routine = null; Finish();
    }
    void PrepareEntry()
    {
        Canvas.ForceUpdateCanvases();
        foreach (var graphic in FindObjectsByType<Graphic>(FindObjectsSortMode.None))
        {
            if (!graphic.enabled || graphic.transform.IsChildOf(transform)) continue;
            var image = graphic as Image;
            if (image != null && image.sprite != null && image.sprite.texture == paperTexture) continue;
            var state = new EntryState { graphic = graphic, original = graphic.material, text = graphic as TMP_Text };
            if (state.text != null) { state.visible = state.text.maxVisibleCharacters; state.text.ForceMeshUpdate(); state.text.maxVisibleCharacters = 0; }
            else if (graphic is Image)
            {
                state.temporary = new Material(entryMaterial); state.temporary.SetFloat("_Progress", 0);
                state.temporary.SetFloat("_Mode", image.sprite != null && image.sprite.name.Contains("frame") || image.sprite != null && image.sprite.name.Contains("outline") ? 0 : 2);
                var rect = graphic.rectTransform.rect; state.temporary.SetVector("_DrawingRect", new Vector4(rect.xMin, rect.yMin, rect.width, rect.height)); graphic.material = state.temporary;
            }
            entryStates.Add(state);
        }
        foreach (var sprite in FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
        {
            if (sprite.sprite == null || sprite.sprite.texture == paperTexture || !sprite.enabled) continue;
            entryStates.Add(new EntryState { sprite = sprite, color = sprite.color }); var color = sprite.color; color.a = 0; sprite.color = color;
        }
    }
    void TickEntry(float p)
    {
        foreach (var state in entryStates)
        {
            if (state.text != null) state.text.maxVisibleCharacters = Mathf.Min(state.visible, Mathf.CeilToInt(state.text.textInfo.characterCount * p));
            else if (state.temporary != null) state.temporary.SetFloat("_Progress", p);
            if (state.sprite != null) { var color = state.color; color.a *= p; state.sprite.color = color; }
        }
    }
    void RestoreEntry()
    {
        foreach (var state in entryStates)
        {
            if (state.text != null) state.text.maxVisibleCharacters = state.visible;
            if (state.graphic != null && state.temporary != null) state.graphic.material = state.original;
            if (state.temporary != null) Destroy(state.temporary);
            if (state.sprite != null) state.sprite.color = state.color;
        }
        entryStates.Clear();
    }
    void SceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!busy) return;
        sceneArrived = true; destination = null; incomingDuration = scene.name == "SampleScene" ? categoryEntryDuration : gameEntryDuration; BindCamera();
        LockInputs();
    }
    bool ReadyForActivation()
    {
#if UNITY_EDITOR
        if (!skipAnimation && ValidationLoadingReady != null && !ValidationLoadingReady()) return false;
#endif
        return loading.progress >= .9f;
    }
    void Update()
    {
        if (busy && screenSize != new Vector2Int(Screen.width, Screen.height)) Complete();
        if (busy && loading == null && !networkScene && sourceCamera != null && Camera.main == sourceCamera && viewportSize != new Vector2Int(sourceCamera.pixelWidth, sourceCamera.pixelHeight)) Complete();
        if (!busy && PhotonNetwork.InRoom && !PhotonNetwork.IsMessageQueueRunning) CoverNetworkLoad(() => { });
    }
    public void Complete()
    {
        skipAnimation = true;
        if (loading != null || networkScene) { if (loading != null) loading.allowSceneActivation = true; RestoreEntry(); return; }
        if (!panelApplied) { destination?.Invoke(); panelApplied = true; }
        CancelImmediate();
    }
    public void CancelNetworkWait() { if (WaitingForNetworkScene) CancelImmediate(); }
    void CancelImmediate() { if (routine != null) StopCoroutine(routine); routine = null; Finish(); }
    void Finish()
    {
        RestoreEntry(); if (overlay != null) { overlay.enabled = false; eraser.enabled = false; loadingLabel.enabled = false; }
        foreach (var state in inputStates) if (state.group != null) state.group.interactable = state.interactable;
        inputStates.Clear();
        busy = false; loading = null; networkScene = false; destination = null; panelApplied = false; Progress = 1; Phase = "Idle";
    }
    void OnApplicationPause(bool paused) { if (paused && busy) Complete(); }
    void OnDisable() { if (Instance == this && busy) Complete(); }
    void OnDestroy()
    {
        if (Instance != this) return;
        SceneManager.sceneLoaded -= SceneLoaded; RestoreEntry(); ReleaseSnapshot(); if (eraseInstance != null) Destroy(eraseInstance); Instance = null;
        foreach (var state in inputStates) if (state.group != null) state.group.interactable = state.interactable;
    }
    void ReleaseSnapshot() { if (snapshot != null) { snapshot.Release(); Destroy(snapshot); snapshot = null; } }
    static void Stretch(RectTransform rect) { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }
}

[RequireComponent(typeof(CanvasRenderer))]
public class PaperEraserGraphic : MaskableGraphic
{
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear(); var r = rectTransform.rect;
        Quad(vh, r, new Color(.84f, .81f, .75f));
        for (int i = 0; i < 4; i++)
        {
            float y = r.yMin + 5 + i * 8;
            Quad(vh, new Rect(r.xMin + 4, y, r.width - 8, .8f), new Color(.35f, .32f, .29f, .3f));
        }
        var ink = new Color(.24f, .22f, .20f, .9f);
        Quad(vh, new Rect(r.xMin, r.yMin, r.width, 1.4f), ink); Quad(vh, new Rect(r.xMin, r.yMax - 1.4f, r.width, 1.4f), ink);
        Quad(vh, new Rect(r.xMin, r.yMin, 1.4f, r.height), ink); Quad(vh, new Rect(r.xMax - 1.4f, r.yMin, 1.4f, r.height), ink);
    }
    static void Quad(VertexHelper vh, Rect r, Color color)
    {
        int n = vh.currentVertCount; vh.AddVert(new Vector2(r.xMin, r.yMin), color, Vector2.zero); vh.AddVert(new Vector2(r.xMin, r.yMax), color, Vector2.zero);
        vh.AddVert(new Vector2(r.xMax, r.yMax), color, Vector2.zero); vh.AddVert(new Vector2(r.xMax, r.yMin), color, Vector2.zero); vh.AddTriangle(n, n + 1, n + 2); vh.AddTriangle(n, n + 2, n + 3);
    }
}
