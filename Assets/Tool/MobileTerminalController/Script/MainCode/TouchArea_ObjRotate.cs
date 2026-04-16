using UnityEngine.EventSystems;
using UnityEngine;
using System.Collections.Generic;

public class TouchArea_ObjRotate : MonoBehaviour, IDragHandler
{
    public CameraController cameraControls;
    public bool Activate = true;
    public void OnDrag( PointerEventData ped )
    {
        if (Input.touchCount <= 0 || !Activate)
            return;
        if (AreaTouch (GetComponent<RectTransform> ()).Count>0)
        {
            Vector2 deltaPos = touchs[0].deltaPosition;
            objRotate (deltaPos);
            if (1 == touchs.Count)
            {
                Touch touch = Input.GetTouch (0);
            }
        }
    }
    /// <summary>
    /// Ðý×ª
    /// </summary>
    /// <param name="mRotateValue"></param>
    void objRotate( Vector2 mRotateValue )
    {
        mRotateValue *= 0.5f;
        cameraControls.CameraRotate (mRotateValue.x, -mRotateValue.y);
    }
    List<Touch> touchs = new List<Touch> ();
    List<Touch> AreaTouch(RectTransform rt)
    {
        touchs.Clear();

        foreach (var item in Input.touches)
        {
            if (RectTransformUtility.RectangleContainsScreenPoint(rt , item.position , null))
            {
                touchs.Add(item);
            }
        }

        return touchs;
    }
}
