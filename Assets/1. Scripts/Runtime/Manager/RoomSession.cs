/// <summary>
/// Current room state across scene loads (JoinRoom → InRoom). Not a MonoBehaviour; no DontDestroyOnLoad.
/// </summary>
public static class RoomSession
{
    public static RoomResponse CurrentRoom { get; private set; }

    public static string LocalUserId { get; private set; }

    public static bool HasRoom =>
        CurrentRoom != null && !string.IsNullOrEmpty(CurrentRoom.roomId);

    public static string RoomId => CurrentRoom?.roomId;

    public static void Enter(RoomResponse room, string localUserId)
    {
        CurrentRoom = room;
        LocalUserId = localUserId ?? string.Empty;
    }

    public static void UpdateRoom(RoomResponse room)
    {
        if (room == null || string.IsNullOrEmpty(room.roomId))
            return;
        if (!HasRoom || CurrentRoom.roomId != room.roomId)
            return;
        CurrentRoom = room;
    }

    /// <summary>Clears in-room state only. Login identity remains on <see cref="NetworkManager"/>.</summary>
    public static void Clear()
    {
        CurrentRoom = null;
    }
}
