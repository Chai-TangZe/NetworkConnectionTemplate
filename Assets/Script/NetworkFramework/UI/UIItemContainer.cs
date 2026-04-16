using UnityEngine;

public class UIItemContainer : MonoBehaviour
{
    [Header("列表容器")]
    [SerializeField] Transform itemsParent;

    public Transform GetItemsParent()
    {
        return itemsParent != null ? itemsParent : transform;
    }

    public void ClearItems()
    {
        Transform parent = GetItemsParent();
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Destroy(parent.GetChild(i).gameObject);
        }
    }
}
