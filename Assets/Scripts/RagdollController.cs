using UnityEngine;
using System.Collections;

public class RagdollController : MonoBehaviour
{
    [System.Serializable]
    public class BodyPart
    {
        public SpriteRenderer spriteRenderer;
        public Collider2D partCollider;
        public Rigidbody2D body;
        public HingeJoint2D joint;
        public PencilStrokeProfile strokeProfile;
        [System.NonSerialized] public Collider2D[] colliders;
        [HideInInspector] public Vector3 originalScale;
        [System.NonSerialized] public bool revealed;
        [System.NonSerialized] public float restAngle;
        [System.NonSerialized] public Color originalColor;
        [System.NonSerialized] public JointAngleLimits2D restLimits;
    }

    [Header("Kafa, gövde, sol kol, sağ kol, sol bacak, sağ bacak")]
    public BodyPart[] partsOrder;
    public PencilDrawEffect drawingEffect;
    public PaperScreenBounds screenBounds;
    private bool initialized;
    private int drawingIndex = -1;
    private float drawingTime;
    private RigidbodyInterpolation2D drawingInterpolation;
    public bool IsDrawing => drawingIndex >= 0;
    public int RevealedCount { get; private set; }
    public Rigidbody2D HeadBody => partsOrder != null && partsOrder.Length > 0 ? partsOrder[0].body : null;

    void Awake() => InitializeParts();

    public void InitializeParts()
    {
        if (initialized || partsOrder == null) return;
        initialized = true;
        foreach (BodyPart part in partsOrder)
        {
            if (part.spriteRenderer == null || part.partCollider == null) continue;
            part.body = part.body != null ? part.body : part.partCollider.attachedRigidbody;
            if (part.body == null) continue;
            part.joint = part.joint != null ? part.joint : part.body.GetComponent<HingeJoint2D>();
            part.originalScale = part.spriteRenderer.transform.localScale;
            part.originalColor = part.spriteRenderer.color;
            part.restAngle = Mathf.DeltaAngle(part.joint != null && part.joint.connectedBody != null ? part.joint.connectedBody.transform.eulerAngles.z : 0f, part.body.transform.eulerAngles.z);
            if (part.joint != null) part.restLimits = part.joint.limits;
            part.body.simulated = false;
            part.colliders = part.body.GetComponents<Collider2D>();
            foreach (var collider in part.colliders) collider.enabled = false;
            if (part.joint != null) part.joint.enabled = false;
            part.spriteRenderer.enabled = false;
        }
        // Overlapping limb endpoints must not kick the character apart.
        for (int i = 0; i < partsOrder.Length; i++)
            for (int j = i + 1; j < partsOrder.Length; j++)
                if (partsOrder[i].colliders != null && partsOrder[j].colliders != null)
                    foreach (var a in partsOrder[i].colliders)
                        foreach (var b in partsOrder[j].colliders) Physics2D.IgnoreCollision(a, b);
    }

    public void AttachToRope(Transform anchor)
    {
        InitializeParts();
        if (anchor == null || HeadBody == null) return;
        BodyPart head = partsOrder[0];
        if (head.joint != null)
        {
            head.joint.autoConfigureConnectedAnchor = false;
            head.joint.connectedBody = anchor.GetComponent<Rigidbody2D>();
            head.joint.connectedAnchor = head.joint.connectedBody != null ? Vector2.zero : (Vector2)anchor.position;
        }
        else
        {
            DistanceJoint2D legacy = HeadBody.GetComponent<DistanceJoint2D>();
            if (legacy != null)
            {
                legacy.connectedBody = anchor.GetComponent<Rigidbody2D>();
                legacy.autoConfigureConnectedAnchor = false;
                legacy.connectedAnchor = legacy.connectedBody != null ? Vector2.zero : (Vector2)anchor.position;
            }
        }
    }

    public void RevealPart(int mistakeCount) => RevealPart(mistakeCount, true);

    public void RevealPart(int mistakeCount, bool animate)
    {
        if (animate && Application.isPlaying) StartCoroutine(DrawPart(mistakeCount));
        else if (CanReveal(mistakeCount)) CompletePart(partsOrder[mistakeCount - 1]);
    }

    bool CanReveal(int mistakeCount)
    {
        InitializeParts();
        int index = mistakeCount - 1;
        if (IsDrawing || partsOrder == null || index < 0 || index >= partsOrder.Length) return false;
        BodyPart part = partsOrder[index];
        return !part.revealed && part.body != null && part.spriteRenderer != null && part.partCollider != null
            && (index == 0 || partsOrder[index == 1 ? 0 : 1].revealed);
    }

    public IEnumerator DrawPart(int mistakeCount)
    {
        if (!BeginDrawing(mistakeCount)) yield break;
        while (IsDrawing)
        {
            yield return null;
            StepDrawing(Time.deltaTime);
        }
    }

    // The same deterministic steps are used by the editor's isolated physics validation.
    public bool BeginDrawing(int mistakeCount)
    {
        if (!CanReveal(mistakeCount)) return false;
        BodyPart part = partsOrder[mistakeCount - 1];
        if (drawingEffect == null || part.strokeProfile == null)
        {
            CompletePart(part);
            return false;
        }
        drawingIndex = mistakeCount - 1;
        drawingTime = 0;
        drawingInterpolation = part.body.interpolation;
        part.body.interpolation = RigidbodyInterpolation2D.None;
        PlaceAtParent(part, false);
        part.spriteRenderer.color = part.originalColor;
        part.spriteRenderer.transform.localScale = part.originalScale;
        part.spriteRenderer.enabled = true;
        drawingEffect.Begin(part.spriteRenderer, part.strokeProfile);
        return true;
    }

    public void StepDrawing(float deltaTime)
    {
        if (!IsDrawing) return;
        BodyPart part = partsOrder[drawingIndex];
        drawingTime += Mathf.Max(0, deltaTime);
        PlaceAtParent(part, false);
        float progress = Mathf.Clamp01(drawingTime / Mathf.Max(0.05f, part.strokeProfile.duration));
        drawingEffect.Tick(progress, deltaTime);
        if (progress < 1) return;
        drawingEffect.Finish();
        part.body.interpolation = drawingInterpolation;
        drawingIndex = -1;
        CompletePart(part);
    }

    void LateUpdate()
    {
        if (!IsDrawing) return;
        BodyPart part = partsOrder[drawingIndex];
        PlaceAtParent(part, false);
        drawingEffect.Tick(Mathf.Clamp01(drawingTime / Mathf.Max(0.05f, part.strokeProfile.duration)), 0);
    }

    void PlaceAtParent(BodyPart part, bool inheritVelocity)
    {
        // Position against the moving parent's current pose, including during a stroke.
        if (part.joint != null)
        {
            Rigidbody2D parent = part.joint.connectedBody;
            float angle = parent != null ? parent.rotation + part.restAngle : part.restAngle;
            Vector2 anchor = parent != null ? parent.GetRelativePoint(part.joint.connectedAnchor) : part.joint.connectedAnchor;
            part.body.rotation = angle;
            Vector2 localAnchor = Quaternion.Euler(0f, 0f, angle) * part.joint.anchor;
            part.body.position = anchor - localAnchor;
            part.body.transform.SetPositionAndRotation(new Vector3(part.body.position.x, part.body.position.y, part.body.transform.position.z), Quaternion.Euler(0, 0, angle));
            if (inheritVelocity)
            {
                part.body.linearVelocity = parent != null ? parent.GetPointVelocity(part.body.position) : Vector2.zero;
                part.body.angularVelocity = parent != null ? parent.angularVelocity : 0f;
            }
        }
    }

    void CompletePart(BodyPart part)
    {
        PencilDrawEffect.ShowComplete(part.spriteRenderer);
        PlaceAtParent(part, true);
        if (part.joint != null) part.joint.enabled = true;
        part.revealed = true;
        RevealedCount++;
        foreach (var collider in part.colliders) collider.enabled = true;
        part.body.simulated = true;
        if (part.joint != null && part.joint.useLimits)
        {
            float reference = part.joint.referenceAngle;
            // HingeJoint2D measures connected-body angle minus this body's angle.
            part.joint.limits = new JointAngleLimits2D {
                min = -part.restAngle - reference - part.restLimits.max,
                max = -part.restAngle - reference - part.restLimits.min
            };
        }
        part.body.WakeUp();
        part.spriteRenderer.enabled = true;
        part.spriteRenderer.transform.localScale = part.originalScale;
        part.spriteRenderer.color = part.originalColor;
    }

    public void AddImpulse(Vector2 impulse)
    {
        if (HeadBody != null && partsOrder[0].revealed) HeadBody.AddForce(impulse, ForceMode2D.Impulse);
    }

    public void Translate(Vector2 delta)
    {
        foreach (var part in partsOrder)
        {
            if (part.body == null) continue;
            part.body.GetComponent<HangmanPart>()?.EndDrag();
            Vector2 position = part.body.position + delta;
            part.body.position = position;
            part.body.transform.position = new Vector3(position.x, position.y, part.body.transform.position.z);
        }
    }

    void OnDisable() => CancelDrawing();

    public void CancelDrawing()
    {
        StopAllCoroutines();
        if (drawingEffect != null) drawingEffect.Stop();
        if (IsDrawing)
        {
            partsOrder[drawingIndex].spriteRenderer.enabled = false;
            partsOrder[drawingIndex].body.interpolation = drawingInterpolation;
            drawingIndex = -1;
        }
    }
}
