public enum ChatEventType
{
    CreateRoom,
    JoinRoom,
    ExitRoom,
    SendMessage,
    WhisperMessage,
    PingPong,
}

public enum JoinRoomResponseType
{
    Success,
    NotExistRoom,
    Fail,
    MaxCount
}

public enum CreateRoomResponseType
{
    Success,
    IsExist,
    Fail
}