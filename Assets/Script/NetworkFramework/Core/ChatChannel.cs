using Mirror;

public interface IChatChannel
{
    void Broadcast(string senderName, string text);
}

public class RoomChatChannel : IChatChannel
{
    readonly RoomManager roomManager;
    readonly IChatTextSanitizer sanitizer;

    public RoomChatChannel(RoomManager roomManager, IChatTextSanitizer sanitizer)
    {
        this.roomManager = roomManager;
        this.sanitizer = sanitizer;
    }

    public void Broadcast(string senderName, string text)
    {
        if (roomManager == null)
        {
            return;
        }

        string safeText = sanitizer != null ? sanitizer.Sanitize(text) : text;
        if (string.IsNullOrWhiteSpace(safeText))
        {
            return;
        }

        foreach (RoomPlayer player in roomManager.GetPlayers())
        {
            if (player != null && player.connectionToClient != null)
            {
                player.TargetReceiveRoomChat(player.connectionToClient, senderName, safeText);
            }
        }
    }
}

public class GameChatChannel : IChatChannel
{
    readonly IChatTextSanitizer sanitizer;

    public GameChatChannel(IChatTextSanitizer sanitizer)
    {
        this.sanitizer = sanitizer;
    }

    public void Broadcast(string senderName, string text)
    {
        string safeText = sanitizer != null ? sanitizer.Sanitize(text) : text;
        if (string.IsNullOrWhiteSpace(safeText))
        {
            return;
        }

        foreach (NetworkConnectionToClient connection in NetworkServer.connections.Values)
        {
            if (connection == null || connection.identity == null)
            {
                continue;
            }

            GamePlayer gamePlayer = connection.identity.GetComponent<GamePlayer>();
            if (gamePlayer != null)
            {
                gamePlayer.TargetReceiveGameChat(connection, senderName, safeText);
            }
        }
    }
}
