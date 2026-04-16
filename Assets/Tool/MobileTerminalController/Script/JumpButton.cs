using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class JumpButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public PlayerMove playerMove;

    public float longPressTime = 5f;
    bool isPressing = false;
    float timer = 0f;

    void Update()
    {
        if (!isPressing) return;

        timer += Time.deltaTime;

        if (timer >= longPressTime)
        {
            isPressing = false;
            OnLongPress();
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        playerMove.OnJump(); // Ô­µã»÷

        isPressing = true;
        timer = 0f;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPressing = false;
        timer = 0f;
    }

    void OnLongPress()
    {
        //SceneDataManager.Instance.PlayerManager.UseTimeScale();
    }
}