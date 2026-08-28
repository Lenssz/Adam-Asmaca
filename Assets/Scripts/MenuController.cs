using UnityEngine;

public class MenuController : MonoBehaviour
{
    // Butonlar artık bu fonksiyona tıklayacak
    public void ChooseGame(int index)
    {
        // Bu aracı script, ölümsüz olan (Instance) GameManager'ı otomatik bulup komutu iletir
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SelectCategory(index);
        }
    }
}