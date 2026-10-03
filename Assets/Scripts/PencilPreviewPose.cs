using UnityEngine;

public class PencilPreviewPose : MonoBehaviour
{
    void Awake()
    {
        gameObject.SetActive(false);
        Destroy(gameObject);
    }
}
