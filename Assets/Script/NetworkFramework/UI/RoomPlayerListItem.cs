using UnityEngine;
using UnityEngine.UI;

public class RoomPlayerListItem : MonoBehaviour
{
    [Header("列表项字段")]
    [SerializeField] Text indexText;
    [SerializeField] Text nameText;
    [SerializeField] Text avatarText;
    [SerializeField] Text readyText;
    [SerializeField] Text leaderText;

    public void SetData(RoomPlayer player)
    {
        if (player == null)
        {
            SetText(indexText, "-");
            SetText(nameText, "未知");
            SetText(avatarText, "形象：--");
            SetText(readyText, "准备：--");
            SetText(leaderText, string.Empty);
            return;
        }

        SetText(indexText, (player.index + 1).ToString());
        SetText(nameText, player.PlayerName);
        SetText(avatarText, $"形象：{player.AvatarId}");
        SetText(readyText, player.readyToBegin ? "已准备" : "未准备");
        SetText(leaderText, player.IsLeader ? "房主" : string.Empty);
    }

    void SetText(Text target, string value)
    {
        if (target != null)
        {
            target.text = value;
        }
    }
}