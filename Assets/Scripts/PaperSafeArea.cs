using UnityEngine;

public class PaperSafeArea : MonoBehaviour
{
    Rect last;
    Vector2Int size;
    void OnEnable() => Refresh();
    void Update()
    {
        if(last != Screen.safeArea || size.x != Screen.width || size.y != Screen.height) Refresh();
    }
    public void Refresh()
    {
        last=Screen.safeArea;size=new Vector2Int(Screen.width,Screen.height);
        var rect=(RectTransform)transform;
        rect.anchorMin=new Vector2(last.xMin/Mathf.Max(1,size.x),last.yMin/Mathf.Max(1,size.y));
        rect.anchorMax=new Vector2(last.xMax/Mathf.Max(1,size.x),last.yMax/Mathf.Max(1,size.y));
        rect.offsetMin=rect.offsetMax=Vector2.zero;
    }
}
