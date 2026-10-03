using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// A separate workshop scene; it does not change words, coins or network state.
public class PencilRagdollPreview : MonoBehaviour
{
    public HangmanDrawer drawer;
    private int shown;
    private Coroutine demonstration;
    void Start() => Restart();
    void Update()
    {
        if (Keyboard.current == null) return;
        if (Keyboard.current.spaceKey.wasPressedThisFrame) RevealNext();
        if (Keyboard.current.rKey.wasPressedThisFrame) Restart();
        if (Keyboard.current.aKey.wasPressedThisFrame) StartDemo();
    }
    public void Restart()
    {
        if (demonstration != null) StopCoroutine(demonstration);
        demonstration = null;
        shown = 0;
        drawer.ResetDrawing();
    }
    void RevealNext() { if (shown < 6) drawer.AddPartToQueue(++shown); }
    void StartDemo() { Restart(); demonstration = StartCoroutine(Demonstrate()); }
    IEnumerator Demonstrate()
    {
        for (int i = 0; i < 6; i++)
        {
            RevealNext();
            yield return drawer.WaitForDrawing();
            if (i < 5) yield return new WaitForSeconds(0.08f);
        }
        demonstration = null;
    }
    void OnGUI()
    {
        float scale = Screen.height / 850f;
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
        GUI.color = new Color(0.15f, 0.14f, 0.13f);
        GUIStyle label = new GUIStyle(GUI.skin.label) { fontSize = 20, alignment = TextAnchor.MiddleCenter };
        float width = Screen.width / scale;
        GUI.Label(new Rect(0, 20, width, 36), "Karakalem çubuk adam · " + shown + "/6", label);
        GUI.Label(new Rect(0, 59, width, 30), "Parçayı tut, sürükle, bırak.", new GUIStyle(label) { fontSize = 15 });
        float bottom = Screen.height / scale - 64;
        if (GUI.Button(new Rect(width / 2 - 185, bottom, 115, 34), "Parça ekle")) RevealNext();
        if (GUI.Button(new Rect(width / 2 - 57, bottom, 115, 34), "Sırayla çiz")) StartDemo();
        if (GUI.Button(new Rect(width / 2 + 71, bottom, 115, 34), "Sıfırla")) Restart();
    }
}
