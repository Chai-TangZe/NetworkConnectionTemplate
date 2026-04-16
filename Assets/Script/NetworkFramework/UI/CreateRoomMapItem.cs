using System;
using UnityEngine;
using UnityEngine.UI;

public class CreateRoomMapItem : MonoBehaviour
{
    [Header("地图项")]
    [SerializeField] Image posterImage;
    [SerializeField] Text mapNameText;
    [SerializeField] Button selectButton;

    int mapIndex;
    Action<int> onSelected;

    public void SetData(Sprite poster, string displayName, int index, Action<int> onClick)
    {
        mapIndex = index;
        onSelected = onClick;

        if (posterImage != null)
        {
            posterImage.sprite = poster;
            posterImage.enabled = poster != null;
        }

        if (mapNameText != null)
        {
            mapNameText.text = displayName;
        }

        if (selectButton != null)
        {
            selectButton.interactable = true;
        }
    }

    public void OnClickSelect()
    {
        onSelected?.Invoke(mapIndex);
    }
}
