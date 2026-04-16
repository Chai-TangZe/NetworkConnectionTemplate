public enum RoomJoinResultCode
{
    Success = 0,
    InvalidRoomId = 1,
    RoomNotFound = 2,
    RoomInGame = 3,
    NetworkManagerMissing = 4,
    Unsupported = 5,
    Waiting = 6,
    Cancelled = 7,
    PasswordIncorrect = 8
}

public class RoomJoinResult
{
    public RoomJoinResultCode Code { get; private set; }
    public string Message { get; private set; }

    RoomJoinResult(RoomJoinResultCode code, string message)
    {
        Code = code;
        Message = message;
    }

    public static RoomJoinResult Ok(string message = "正在加入房间。")
    {
        return new RoomJoinResult(RoomJoinResultCode.Success, message);
    }

    public static RoomJoinResult Fail(RoomJoinResultCode code, string message)
    {
        return new RoomJoinResult(code, message);
    }
}
