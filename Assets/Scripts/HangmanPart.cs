using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class HangmanPart : MonoBehaviour
{
    [Min(0.1f)] public float dragForce = 18f;
    [Min(0.1f)] public float maxDragDistance = 0.7f;
    [Min(0.1f)] public float maxReleaseSpeed = 3.5f;
    private Rigidbody2D body;
    private Collider2D partCollider;
    private Camera cam;
    private TargetJoint2D dragJoint;
    private Vector2 initialPointer;
    private Vector2 initialGrab;
    private bool dragging;
    private bool touchInput;
    private RagdollController rig;
    private SpriteRenderer artwork;
    private readonly List<RaycastResult> uiHits = new List<RaycastResult>();

    void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        partCollider = GetComponent<Collider2D>();
        rig = GetComponentInParent<RagdollController>();
        artwork = GetComponentInChildren<SpriteRenderer>();
        cam = Camera.main;
        dragJoint = GetComponent<TargetJoint2D>();
        if (dragJoint == null) dragJoint = gameObject.AddComponent<TargetJoint2D>();
        dragJoint.enabled = false;
        dragJoint.autoConfigureTarget = false;
        dragJoint.frequency = 4f;
        dragJoint.dampingRatio = 0.85f;
    }

    void Update()
    {
        if (PaperPageTransition.IsTransitioning) { EndDrag(); return; }
        if (cam == null) cam = Camera.main;
        if (cam == null || !body.simulated || !partCollider.enabled) { EndDrag(); return; }
        Vector2 screenPosition;
        bool pressed, held;
        if (dragging && touchInput && Touchscreen.current != null)
        {
            screenPosition = Touchscreen.current.primaryTouch.position.ReadValue();
            pressed = false;
            held = Touchscreen.current.primaryTouch.press.isPressed;
        }
        else if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            screenPosition = Touchscreen.current.primaryTouch.position.ReadValue();
            pressed = Touchscreen.current.primaryTouch.press.wasPressedThisFrame;
            held = true;
            touchInput = true;
        }
        else if (Mouse.current != null)
        {
            screenPosition = Mouse.current.position.ReadValue();
            pressed = Mouse.current.leftButton.wasPressedThisFrame;
            held = Mouse.current.leftButton.isPressed;
            if (!dragging) touchInput = false;
        }
        else { EndDrag(); return; }
        Vector2 worldPosition = cam.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, -cam.transform.position.z));
        if (pressed && !dragging)
        {
            if (OverInteractiveUI(screenPosition)) return;
            Collider2D hit = Physics2D.OverlapPoint(worldPosition);
            if (hit == null || hit.attachedRigidbody != body) return;
            initialPointer = worldPosition;
            initialGrab = worldPosition;
            dragJoint.anchor = transform.InverseTransformPoint(worldPosition);
            dragJoint.target = worldPosition;
            dragJoint.maxForce = dragForce * body.mass;
            dragJoint.enabled = true;
            dragging = true;
            body.WakeUp();
        }
        if (!dragging) return;
        if (!held) { EndDrag(); return; }
        Vector2 target = initialGrab + Vector2.ClampMagnitude(worldPosition - initialPointer, maxDragDistance);
        dragJoint.target = rig != null && rig.screenBounds != null ? rig.screenBounds.ClampTarget(body, dragJoint.anchor, target, artwork) : target;
    }

    bool OverInteractiveUI(Vector2 position)
    {
        if (EventSystem.current == null) return false;
        uiHits.Clear();
        EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = position }, uiHits);
        // Paper/background Images can cover the scene without blocking the character.
        foreach (var hit in uiHits)
            if (hit.gameObject.GetComponentInParent<Selectable>() != null) return true;
        return false;
    }

    public void EndDrag()
    {
        if (!dragging) return;
        dragging = false;
        if (dragJoint != null) dragJoint.enabled = false;
        if (body != null && body.simulated)
        {
            body.linearVelocity = Vector2.ClampMagnitude(body.linearVelocity, maxReleaseSpeed);
            body.angularVelocity = Mathf.Clamp(body.angularVelocity, -220f, 220f);
        }
    }
    void OnDisable() => EndDrag();
    void OnApplicationFocus(bool focused) { if (!focused) EndDrag(); }
}
