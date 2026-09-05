using UnityEngine;
using UnityEngine.InputSystem; // Yeni Input sistemi kütüphanesi

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class HangmanPart : MonoBehaviour
{
    private Rigidbody2D rb;
    private Camera cam;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        cam = Camera.main;
    }

    void Update()
    {
        // Yeni Input sistemine göre sol týklama kontrolü
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
            Vector2 mousePos = cam.ScreenToWorldPoint(mouseScreenPos);
            RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);

            if (hit.collider != null && hit.collider.gameObject == gameObject)
            {
                Debug.Log("YENÝ SÝSTEMLE TIKLANDI: " + gameObject.name);

                if (rb != null)
                {
                    float randomForce = Random.Range(-8f, 8f);
                    rb.AddForce(new Vector2(randomForce, 0), ForceMode2D.Impulse);
                    rb.AddTorque(randomForce * 0.1f, ForceMode2D.Impulse);
                }
            }
        }
    }
}