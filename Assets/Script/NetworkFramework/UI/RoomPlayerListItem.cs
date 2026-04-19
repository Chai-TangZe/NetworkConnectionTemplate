using UnityEngine;
using UnityEngine.UI;

public class RoomPlayerListItem : MonoBehaviour
{
    [Header("列表项字段")]
    [SerializeField] Text indexText;
    [SerializeField] Text nameText;
    [SerializeField] Text descText;
    [SerializeField] Image avatarImage;
    [SerializeField] Text readyText;
    [SerializeField] Text leaderText;

    public void SetData(RoomPlayer player)
    {
        if (player == null)
        {
            SetText(indexText, "-");
            SetText(nameText, "未知");
            SetText(descText, string.Empty);
            SetAvatarImage(null);
            SetText(readyText, "准备：--");
            SetText(leaderText, string.Empty);
            return;
        }

        SetText(indexText, (player.index + 1).ToString());
        SetText(nameText, player.PlayerName);
        SetText(descText, string.IsNullOrWhiteSpace(player.PlayerDescription) ? string.Empty : player.PlayerDescription);
        Sprite icon = AvatarDataRepository.GetIconOrNull(player.AvatarId);
        SetAvatarImage(icon);
        SetText(readyText, player.IsReady ? "已准备" : "未准备");
        SetText(leaderText, player.IsLeader ? "房主" : "玩家");
    }

    void SetAvatarImage(Sprite sprite)
    {
        if (avatarImage == null)
        {
            return;
        }

        avatarImage.sprite = sprite;
        avatarImage.enabled = sprite != null;
    }

    void SetText(Text target, string value)
    {
        if (target != null)
        {
            target.text = value;
        }
    }
}