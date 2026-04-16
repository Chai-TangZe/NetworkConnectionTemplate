using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PanelBase : MonoBehaviour
{
    CanvasGroup CanvasGroup_Panel;
    bool ActivateChangeAlpha = false;
    float AlphaValue = 0;
    [HideInInspector]
    public bool ActivateState=false;
    protected virtual void Start()
    {
        CanvasGroup_Panel = GetComponent<CanvasGroup>();
    }
    public virtual void ShowPanel()
    {
        AlphaValue = 1;
        ActivateChangeAlpha = true;
        CanvasGroup_Panel.blocksRaycasts = true;
        ActivateState = true;
    }
    public virtual void HidePanel()
    {
        AlphaValue = 0;
        ActivateChangeAlpha = true;
        CanvasGroup_Panel.blocksRaycasts = false ;
        ActivateState = false;
    }
    public void ImmediatelyHide()
    {
        AlphaValue = 0;
        CanvasGroup_Panel.blocksRaycasts = false;
        ActivateState = false;
        CanvasGroup_Panel.alpha = 0;
    }
    protected virtual void Update()
    {
        if (ActivateChangeAlpha)
        {
            CanvasGroup_Panel.alpha = Mathf.Lerp(CanvasGroup_Panel.alpha, AlphaValue, 0.1f);
            if (Mathf.Abs(CanvasGroup_Panel.alpha - AlphaValue) < .1f)
            {
                CanvasGroup_Panel.alpha = AlphaValue;
                ActivateChangeAlpha = false;
            }
        }
    }
}
