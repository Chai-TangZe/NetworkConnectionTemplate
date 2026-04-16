using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;


public class VJHandler : MonoBehaviour, IDragHandler, IPointerUpHandler, IPointerDownHandler
{
    private Image jsContainer;
    private Image joystick;
    [Header ("输入的方向")]
    public Vector3 InputDirection;
    [Header ("距离中心的距离")]
    public float CentreDistance = 1;
    void init()
    {
        jsContainer = GetComponent<Image> ();
        if (!jsContainer)
            Debug.Log ("没有找到底图");
        if (transform.childCount == 0)
            Debug.Log ("没有找到触摸点");
        joystick = transform.GetChild (0).GetComponent<Image> ();
        if (!joystick)
            Debug.Log ("没有找到触摸点图");
        InputDirection = Vector3.zero;
    }
    public void OnDrag( PointerEventData ped )
    {
        Vector2 position = Vector2.zero;
        RectTransformUtility.ScreenPointToLocalPointInRectangle
                (jsContainer.rectTransform,
                ped.position,
                ped.pressEventCamera,
                out position);

        position.x = ( position.x / jsContainer.rectTransform.sizeDelta.x );
        position.y = ( position.y / jsContainer.rectTransform.sizeDelta.y );

        float x = ( jsContainer.rectTransform.pivot.x == 1f ) ? position.x * 2 + 1 : position.x * 2 - 1;
        float y = ( jsContainer.rectTransform.pivot.y == 1f ) ? position.y * 2 + 1 : position.y * 2 - 1;

        InputDirection = new Vector3 (x, y, 0);
        InputDirection = ( InputDirection.magnitude > 1 ) ? InputDirection.normalized : InputDirection;

        joystick.rectTransform.anchoredPosition = new Vector3 (InputDirection.x * ( ( jsContainer.rectTransform.sizeDelta.x / 2 ) * CentreDistance )
                                                               , InputDirection.y * ( ( jsContainer.rectTransform.sizeDelta.y ) / 2 ) * CentreDistance);
    }

    public void OnPointerDown( PointerEventData ped )
    {
        if (!jsContainer)
            init ();
        OnDrag (ped);
    }

    public void OnPointerUp( PointerEventData ped )
    {
        InputDirection = Vector3.zero;
        joystick.rectTransform.anchoredPosition = Vector3.zero;
    }
}
