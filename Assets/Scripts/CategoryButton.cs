using UnityEngine;
using UnityEngine.UI;

public class CategoryButton : MonoBehaviour
{
    [Tooltip("0 = LoL, 1 = Valorant, 2 = CS")]
    public int categoryIndex = 0;

    void Start()
    {
        GetComponent<Button>().onClick.AddListener(() =>
        {
            GameManager.Instance.SelectCategory(categoryIndex);
        });
    }
}
