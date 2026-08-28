using UnityEngine;

/// <summary>
/// Bu script GameScene'deki herhangi bir GameObject'e eklenir.
/// Sahne yüklenince GameManager'a haber verir.
/// </summary>
public class GameSceneBootstrap : MonoBehaviour
{
    void Start()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("[Bootstrap] GameManager bulunamadı! MainScene'den başlatıldığından emin ol.");
            return;
        }
        GameManager.Instance.StartGame();
    }
}
