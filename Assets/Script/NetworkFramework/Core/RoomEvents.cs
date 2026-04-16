using System;

public static class RoomEvents
{
    public static Action OnPlayerListChanged;
    public static Action<RoomPlayer> OnPlayerUpdated;
    public static Action OnRoomInfoChanged;
    public static Action<bool> OnRoomStateChanged;
    public static Action<string> OnRoomChatMessage;
    public static Action<string> OnGameChatMessage;

    public static void RaisePlayerListChanged()
    {
        OnPlayerListChanged?.Invoke();
    }

    public static void RaisePlayerUpdated(RoomPlayer player)
    {
        OnPlayerUpdated?.Invoke(player);
    }

    public static void RaiseRoomInfoChanged()
    {
        OnRoomInfoChanged?.Invoke();
    }

    public static void RaiseRoomStateChanged(bool isPlaying)
    {
        OnRoomStateChanged?.Invoke(isPlaying);
    }

    public static void RaiseRoomChatMessage(string message)
    {
        OnRoomChatMessage?.Invoke(message);
    }

    public static void RaiseGameChatMessage(string message)
    {
        OnGameChatMessage?.Invoke(message);
    }
}
