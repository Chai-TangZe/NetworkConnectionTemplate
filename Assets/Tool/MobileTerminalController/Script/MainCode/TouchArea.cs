using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TouchArea : MonoBehaviour, IDragHandler
{
    public Text text;
    public Text text1;
    RectTransform rectTransform;
    
    public void OnDrag( PointerEventData ped )
    {
        if (!rectTransform)
            rectTransform = GetComponent<RectTransform> ();
        if (Input.touchCount <= 0 )
            return;

    }
    List<Touch> touchs = new List<Touch> ();
    List<Touch> AreaTouch( RectTransform rt)
    {
        touchs.Clear ();
        foreach (var item in Input.touches)
        {
            if (item.position.x > rt.anchoredPosition.x - ( rt.sizeDelta.x / 2 ) &&
                item.position.x < rt.anchoredPosition.x + ( rt.sizeDelta.x / 2 ) &&
                item.position.y < rt.anchoredPosition.y + ( rt.sizeDelta.y / 2 ) &&
                item.position.y > rt.anchoredPosition.y - ( rt.sizeDelta.y / 2 ))
            {
                touchs.Add (item);
            }
        }
        return touchs;
    }
}
