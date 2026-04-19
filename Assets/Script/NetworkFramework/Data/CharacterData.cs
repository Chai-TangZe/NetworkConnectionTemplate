using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 单个游戏角色外观与装扮数据，隶属于用户（与账号头像分离）。
/// 身体、装饰等使用 <see cref="PlayerPartData"/> 的 Slot 区分，例如 <see cref="CharacterPartSlots"/>。
/// </summary>
[Serializable]
public class CharacterData
{
    /// <summary>角色实例 Id（本地或服务器分配）。</summary>
    public string CharacterId;

    /// <summary>部位装扮：身体、装饰A/B 等。</summary>
    public List<PlayerPartData> Parts = new List<PlayerPartData>();

    public string GetPartItemId(string slot)
    {
        if (string.IsNullOrWhiteSpace(slot) || Parts == null)
        {
            return null;
        }

        PlayerPartData entry = Parts.FirstOrDefault(p => p != null && p.Slot == slot);
        return entry != null ? entry.ItemId : null;
    }

    public void SetPartItemId(string slot, string itemId)
    {
        if (string.IsNullOrWhiteSpace(slot))
        {
            return;
        }

        Parts ??= new List<PlayerPartData>();
        PlayerPartData entry = Parts.FirstOrDefault(p => p != null && p.Slot == slot);
        if (entry == null)
        {
            entry = new PlayerPartData { Slot = slot };
            Parts.Add(entry);
        }

        entry.ItemId = itemId;
    }
}
