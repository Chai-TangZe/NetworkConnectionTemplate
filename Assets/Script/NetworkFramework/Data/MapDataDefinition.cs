using UnityEngine;

[CreateAssetMenu(fileName = "MapData_", menuName = "NetworkFramework/Map Data")]
public class MapDataDefinition : ScriptableObject
{
    public GameMapType mapType;
    public string displayName;
    public Sprite poster;
    [Tooltip("该地图对应的游戏场景名（可填短名或 .unity 路径）。为空时回退 RoomManager.GameplayScene。")]
    public string gameplaySceneName;
}
