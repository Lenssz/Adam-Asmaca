using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Keep long Turkish words inside their allotted row at every aspect ratio.
public class PaperWordLayout : MonoBehaviour
{
    int lastCount = -1;
    float lastWidth;
    void LateUpdate()
    {
        var rect=(RectTransform)transform;
        if(lastCount==transform.childCount && Mathf.Approximately(lastWidth,rect.rect.width)) return;
        lastCount=transform.childCount;lastWidth=rect.rect.width;
        if(lastCount==0) return;
        var layout=GetComponent<HorizontalLayoutGroup>();
        float spacing=layout!=null ? layout.spacing : 8;
        float padding=layout!=null ? layout.padding.horizontal : 0;
        float width=Mathf.Min(72,Mathf.Max(8,(lastWidth-padding-spacing*(lastCount-1))/lastCount));
        if(layout!=null){layout.childControlWidth=true;layout.childForceExpandWidth=false;}
        foreach(Transform child in transform)
        {
            var element=child.GetComponent<LayoutElement>()??child.gameObject.AddComponent<LayoutElement>();
            element.minWidth=0;element.preferredWidth=width;element.flexibleWidth=0;
            var text=child.GetComponentInChildren<TMP_Text>();
            if(text!=null){text.enableAutoSizing=true;text.fontSizeMin=8;text.fontSizeMax=Mathf.Min(48,width*.85f);}
        }
        LayoutRebuilder.MarkLayoutForRebuild(rect);
    }
}
