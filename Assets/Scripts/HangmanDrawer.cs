using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class HangmanDrawer : MonoBehaviour
{
    [Header("Fizikli Sistem")]
    public GameObject ragdollPrefab;
    public Transform ropeTipAnchor;

    private GameObject currentRagdoll;
    private RagdollController currentRagdollController;
    private Queue<int> pendingParts = new Queue<int>();
    private bool isDrawing = false;

    void OnEnable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnLetterGuessed += OnLetterGuessed;
            GameManager.Instance.OnWordChanged += ResetDrawing;
        }
    }

    void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnLetterGuessed -= OnLetterGuessed;
            GameManager.Instance.OnWordChanged -= ResetDrawing;
        }
    }

    public void AddPartToQueue(int wrongCount)
    {
        pendingParts.Enqueue(wrongCount);
        if (!isDrawing) StartCoroutine(ProcessDrawingQueue());
    }

    void OnLetterGuessed(char letter, bool correct)
    {
        if (!correct && GameManager.Instance != null)
        {
            AddPartToQueue(GameManager.Instance.WrongGuessCount);
        }
    }

    IEnumerator ProcessDrawingQueue()
    {
        isDrawing = true;
        while (pendingParts.Count > 0)
        {
            int partToDraw = pendingParts.Dequeue();
            if (currentRagdollController != null)
            {
                currentRagdollController.RevealPart(partToDraw);
            }
            yield return new WaitForSeconds(0.4f);
        }
        isDrawing = false;
    }

    public void ResetDrawing()
    {
        StopAllCoroutines();
        pendingParts.Clear();
        isDrawing = false;

        if (currentRagdoll != null) Destroy(currentRagdoll);

        if (ragdollPrefab != null && ropeTipAnchor != null)
        {
            currentRagdoll = Instantiate(ragdollPrefab, ropeTipAnchor.position, Quaternion.identity);
            currentRagdollController = currentRagdoll.GetComponent<RagdollController>();

            // Kafadaki DistanceJoint2D'yi bul ve sahnedeki RopeTip'i KODLA bağla
            DistanceJoint2D headJoint = currentRagdollController.partsOrder[0].spriteRenderer.GetComponent<DistanceJoint2D>();
            if (headJoint != null)
            {
                headJoint.connectedBody = ropeTipAnchor.GetComponent<Rigidbody2D>();
                headJoint.autoConfigureDistance = false; // Kodla bağlarken mesafenin bozulmaması için
            }
        }
    }

    public void PlayLoseEffect()
    {
        StartCoroutine(ShakeEffect());
    }

    IEnumerator ShakeEffect()
    {
        // UI titremesi yerine ekranı (kamerayı) titretiyoruz
        Vector3 original = Camera.main.transform.localPosition;
        for (int i = 0; i < 12; i++)
        {
            Camera.main.transform.localPosition = original + new Vector3(Random.Range(-0.2f, 0.2f), Random.Range(-0.2f, 0.2f), 0);
            yield return new WaitForSeconds(0.05f);
        }
        Camera.main.transform.localPosition = original;
    }
}