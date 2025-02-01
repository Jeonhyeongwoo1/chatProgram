namespace Chatting.Enum
{
    public enum CreateRoomResponseType
    {
        Success,
        IsExist,
        Fail
    }

    public enum JoinRoomResponseType
    {
        Success,
        NotExistRoom,
        Fail,
        MaxCount
    }
    public enum ChatEventType
    {
        CreateRoom,
        JoinRoom,
        ExitRoom,
        SendMessage,
        PingPong
    }
}
