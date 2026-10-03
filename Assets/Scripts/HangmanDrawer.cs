using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class HangmanDrawer : MonoBehaviour
{
    [Header("Fizikli sistem")]
    public GameObject ragdollPrefab;
    public Transform ropeTipAnchor;
    public PaperScreenBounds screenBounds;
    [Min(0f)] public float partInterval = 0.08f;
    private GameObject currentRagdoll;
    private RagdollController currentRagdollController;
    private readonly Queue<int> pendingParts = new Queue<int>();
    private int highestQueuedPart;
    private bool isDrawing;
    private Coroutine shakeRoutine;
    private bool pendingLoseEffect;
    private Camera shakenCamera;
    private Vector3 originalCameraPosition;
    public RagdollController CurrentRagdoll => currentRagdollController;
    public bool IsDrawing => isDrawing || pendingParts.Count > 0;
    public int DrawingRevision { get; private set; }

    public IEnumerator WaitForDrawing()
    {
        while (IsDrawing) yield return null;
    }

    void OnEnable()
    {
        if (GameManager.Instance == null) return;
        GameManager.Instance.OnLetterGuessed += OnLetterGuessed;
        GameManager.Instance.OnWordChanged += ResetDrawing;
    }
    void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnLetterGuessed -= OnLetterGuessed;
            GameManager.Instance.OnWordChanged -= ResetDrawing;
        }
        StopAllCoroutines();
        pendingParts.Clear();
        highestQueuedPart = 0;
        isDrawing = false;
        pendingLoseEffect = false;
        DrawingRevision++;
        RestoreCamera();
        DestroyRagdoll();
    }
    public void AddPartToQueue(int wrongCount)
    {
        if (currentRagdollController == null || wrongCount <= highestQueuedPart) return;
        int max = Mathf.Min(wrongCount, currentRagdollController.partsOrder.Length);
        for (int i = highestQueuedPart + 1; i <= max; i++) pendingParts.Enqueue(i);
        highestQueuedPart = max;
        if (!isDrawing && pendingParts.Count > 0) StartCoroutine(ProcessDrawingQueue());
    }
    void OnLetterGuessed(char letter, bool correct)
    {
        if (!correct && GameManager.Instance != null) AddPartToQueue(GameManager.Instance.WrongGuessCount);
    }
    IEnumerator ProcessDrawingQueue()
    {
        isDrawing = true;
        while (pendingParts.Count > 0)
        {
            int part = pendingParts.Dequeue();
            if (currentRagdollController != null) yield return currentRagdollController.DrawPart(part);
            if (pendingParts.Count > 0 && partInterval > 0) yield return new WaitForSeconds(partInterval);
        }
        isDrawing = false;
        if (pendingLoseEffect) { pendingLoseEffect = false; PlayLoseEffect(); }
    }
    public void ResetDrawing()
    {
        StopAllCoroutines();
        RestoreCamera();
        pendingParts.Clear();
        highestQueuedPart = 0;
        isDrawing = false;
        pendingLoseEffect = false;
        DrawingRevision++;
        DestroyRagdoll();
        if (ragdollPrefab == null || ropeTipAnchor == null) return;
        currentRagdoll = Instantiate(ragdollPrefab, ropeTipAnchor.position, Quaternion.identity);
        currentRagdollController = currentRagdoll.GetComponent<RagdollController>();
        if (currentRagdollController != null)
        {
            currentRagdollController.screenBounds = screenBounds;
            currentRagdollController.AttachToRope(ropeTipAnchor);
        }
    }
    void DestroyRagdoll()
    {
        if (currentRagdoll != null) { currentRagdoll.SetActive(false); Destroy(currentRagdoll); }
        currentRagdoll = null;
        currentRagdollController = null;
    }
    public void PlayLoseEffect()
    {
        if (IsDrawing) { pendingLoseEffect = true; return; }
        if (shakeRoutine != null) return;
        currentRagdollController?.AddImpulse(new Vector2(0.035f, 0f));
        shakeRoutine = StartCoroutine(ShakeEffect());
    }
    IEnumerator ShakeEffect()
    {
        shakenCamera = Camera.main;
        if (shakenCamera == null) { shakeRoutine = null; yield break; }
        originalCameraPosition = shakenCamera.transform.localPosition;
        for (int i = 0; i < 10; i++)
        {
            float strength = 0.07f * (1f - i / 10f);
            shakenCamera.transform.localPosition = originalCameraPosition + (Vector3)(Random.insideUnitCircle * strength);
            yield return new WaitForSeconds(0.04f);
        }
        RestoreCamera();
    }
    void RestoreCamera()
    {
        if (shakenCamera != null) shakenCamera.transform.localPosition = originalCameraPosition;
        shakenCamera = null;
        shakeRoutine = null;
    }
}
