using UnityEngine;

/// <summary>
/// 角色定制面板：身体、装饰A、装饰B 等 Tab + 选项网格。
/// 数据读写 <see cref="PlayerProfileContext.Character"/> 的 <see cref="CharacterData.Parts"/>（槽位名见 <see cref="CharacterPartSlots"/>）。
/// 与「人物信息」用户面板分离；具体 UI 绑定在编辑器中完成。
/// </summary>
public class CharacterPanelUI : MonoBehaviour
{
    [Header("面板")]
    [SerializeField] GameObject panelRoot;

    public void Open()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(true);
        }

        PlayerProfileContext context = PlayerProfileContext.Instance ?? PlayerProfileContext.EnsureInstance();
        context.EnsureDefaults();
    }

    public void Close()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }
    }
}
