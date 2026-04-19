using System;
using UnityEngine;
using UnityEngine.UI;

public class AvatarPickerItem : MonoBehaviour
{
    [SerializeField] Button button;
    [SerializeField] Image iconImage;

    Action<int> onSelected;
    int avatarId;

    void Awake()
    {
        if (button != null)
        {
            button.onClick.AddListener(OnClick);
        }
    }

    public void SetData(AvatarDataDefinition data, Action<int> callback)
    {
        onSelected = callback;
        avatarId = data != null ? data.avatarId : 0;

        if (iconImage != null)
        {
            iconImage.sprite = data != null ? data.icon : null;
            iconImage.enabled = data != null && data.icon != null;
        }
    }

    void OnClick()
    {
        onSelected?.Invoke(avatarId);
    }
}
