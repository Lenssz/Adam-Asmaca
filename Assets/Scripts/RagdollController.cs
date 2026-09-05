using UnityEngine;
using DG.Tweening;

public class RagdollController : MonoBehaviour
{
    [System.Serializable]
    public class BodyPart
    {
        public SpriteRenderer spriteRenderer;
        public Collider2D partCollider;
        [HideInInspector] public Vector3 originalScale; // Sizin ayarladığınız boyutu tutar
    }

    [Header("Sırasıyla: Kafa, Gövde, Sol Kol, Sağ Kol, Sol Bacak, Sağ Bacak")]
    public BodyPart[] partsOrder;

    void Awake()
    {
        foreach (var part in partsOrder)
        {
            if (part.spriteRenderer != null)
            {
                part.originalScale = part.spriteRenderer.transform.localScale;
                part.spriteRenderer.enabled = false;
                if (part.partCollider != null) part.partCollider.enabled = false;
                part.spriteRenderer.transform.localScale = Vector3.zero;
            }
        }
    }

    public void RevealPart(int mistakeCount)
    {
        int index = mistakeCount - 1;
        if (index >= 0 && index < partsOrder.Length)
        {
            BodyPart part = partsOrder[index];
            part.spriteRenderer.enabled = true;
            part.partCollider.enabled = true;

            // 3. Sabit 1 yerine, sizin orijinal boyutunuza büyüt
            part.spriteRenderer.transform.DOScale(part.originalScale, 0.4f).SetEase(Ease.OutBack);
        }
    }
}