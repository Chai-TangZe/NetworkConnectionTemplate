using UnityEngine;

[CreateAssetMenu(fileName = "AvatarData_", menuName = "NetworkFramework/Avatar Data")]
public class AvatarDataDefinition : ScriptableObject
{
    [Tooltip("与用户档案 UserData.AvatarId 对应，需唯一；0 为默认头像。")]
    public int avatarId;

    public string displayName;
    public Sprite icon;
}
