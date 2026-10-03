using UnityEngine;

[DefaultExecutionOrder(-150)]
public class PaperScenePresentation : MonoBehaviour
{
    public PaperScreenBounds screenBounds;
    public SpriteRenderer paper;
    public SpriteRenderer gallows;
    public Transform ropeAnchor;
    public HangmanDrawer drawer;
    public Vector2 ropeTipUV = new Vector2(.82f, .80f);
    public Vector2 beamUV = new Vector2(.82f, .90f);
    public Vector2 postUV = new Vector2(.18f, .90f);
    public Vector2 gallowsSize = new Vector2(1.36f, 3.10f);

    void OnEnable()
    {
        if (screenBounds == null) return;
        screenBounds.BoundsChanged += RefreshPresentation;
        screenBounds.RefreshNow();
    }
    void OnDisable() { if (screenBounds != null) screenBounds.BoundsChanged -= RefreshPresentation; }

    public void RefreshPresentation()
    {
        if (paper == null || gallows == null || ropeAnchor == null) return;
        Rect full = screenBounds.FullRect, safe = screenBounds.InnerRect;
        paper.transform.position = new Vector3(full.center.x, full.center.y, 1);
        paper.transform.localScale = new Vector3(full.width / paper.sprite.bounds.size.x, full.height / paper.sprite.bounds.size.y, 1);
        Vector2 rope = new Vector2(safe.xMin + .20f + (ropeTipUV.x - postUV.x) * gallowsSize.x,
            safe.yMax - .16f - (beamUV.y - ropeTipUV.y) * gallowsSize.y);
        Vector2 delta = rope - (Vector2)ropeAnchor.position;
        var anchorBody = ropeAnchor.GetComponent<Rigidbody2D>();
        if (anchorBody != null) anchorBody.position = rope;
        ropeAnchor.position = rope;
        gallows.transform.localScale = new Vector3(gallowsSize.x / gallows.sprite.bounds.size.x, gallowsSize.y / gallows.sprite.bounds.size.y, 1);
        Vector2 offset = Vector2.Scale(new Vector2(.5f,.5f) - ropeTipUV, gallowsSize);
        gallows.transform.position = rope + offset;
        // A viewport change translates the existing rig with its anchor; it does not reset mistakes.
        if (drawer != null && drawer.CurrentRagdoll != null && delta.sqrMagnitude > .000001f)
            drawer.CurrentRagdoll.Translate(delta);
    }
}
