using UnityEngine;
using UnityEngine.EventSystems;


public class TouchArea_ObjZoom : MonoBehaviour, IDragHandler, IPointerUpHandler
{
    public Transform Obj;
    public bool Activate = true;

    private Touch oldTouch1;  //上次触摸点1(手指1)  
    private Touch oldTouch2;  //上次触摸点2(手指2)  
    Touch newTouch1;
    Touch newTouch2;
    public void OnDrag( PointerEventData ped )
    {
        if (Input.touchCount <= 0 || !Activate)
            return;
        
        if (2 == Input.touchCount)
        {
            newTouch1 = Input.GetTouch (0);
            newTouch2 = Input.GetTouch (1);

            if (newTouch2.phase == TouchPhase.Began)
            {
                oldTouch2 = newTouch2;
                oldTouch1 = newTouch1;
                return;
            }

            //计算老的两点距离和新的两点间距离，变大要放大模型，变小要缩放模型  
            float oldDistance = Vector2.Distance (oldTouch1.position, oldTouch2.position);
            float newDistance = Vector2.Distance (newTouch1.position, newTouch2.position);

            //两个距离之差，为正表示放大手势， 为负表示缩小手势  
            float offset = newDistance - oldDistance;
            objZoom (offset);
        }
    }

    public void OnPointerUp( PointerEventData ped )
    {
        //记住最新的触摸点，下次使用  
        oldTouch1 = newTouch1;
        oldTouch2 = newTouch2;
    }
    
    void objZoom( float mZoomValue )
    {
        //放大因子， 一个像素按 0.01倍来算(100可调整)  
        float scaleFactor = mZoomValue / 100f;
        Vector3 localScale = Obj.localScale;
        Vector3 scale = new Vector3 (localScale.x + scaleFactor,
                                    localScale.y + scaleFactor,
                                    localScale.z + scaleFactor);

        //最小缩放到 0.3 倍  
        if (scale.x > 0.1f && scale.y > 0.1f && scale.z > 0.1f)
        {
            Obj.localScale = scale;
        }
    }
}
