using Mirror;

public interface IRoomJoinPolicy
{
    RoomJoinResult Validate(RoomData roomData);
}

public class RoomJoinPolicy : IRoomJoinPolicy
{
    public RoomJoinResult Validate(RoomData roomData)
    {
        if (roomData == null)
        {
            return RoomJoinResult.Fail(RoomJoinResultCode.RoomNotFound, "加入失败：未找到目标房间。");
        }

        if (roomData.IsPlaying)
        {
            return RoomJoinResult.Fail(RoomJoinResultCode.RoomInGame, $"房间正在游戏中：{roomData.RoomName}");
        }

        if (NetworkManager.singleton == null)
        {
            return RoomJoinResult.Fail(RoomJoinResultCode.NetworkManagerMissing, "加入失败：未找到 NetworkManager。");
        }

        return RoomJoinResult.Ok();
    }
}
