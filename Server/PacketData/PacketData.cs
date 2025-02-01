using System.Net.WebSockets;
using Chatting.Enum;

namespace Chatting
{
    public class WebSocketRequestData
    {
        public string eventType;
        public string jsonString;
        public WebSocketMessageType webSocketMessageType = WebSocketMessageType.Text;
    }

    public class WebSocketResponseData
    {
        public string eventType;
        public string jsonString;
        public WebSocketMessageType webSocketMessageType = WebSocketMessageType.Text;
    }

    public class BaseRequestData
    {

    }

    public class BaseResponseData
    {

    }

    public class ChatDataReq : BaseRequestData
    {
        public string userName;
        public string message;
    }

    public class ChatDataRes : BaseResponseData
    {
        public string userName;
        public string message;
    }

    public class CreateRoomReq : BaseRequestData
    {
        public UserData userData;
        public string roomName;
    }

    public class CreateRoomRes : BaseResponseData
    {
        public UserData userData;
        public string roomName;
        public CreateRoomResponseType createRoomResponseType;
    }

    public class JoinRoomReq : BaseRequestData
    {
        public UserData userData;
        public string roomName;
    }

    public class JoinRoomRes : BaseResponseData
    {
        public string roomName;
        public List<UserData> userDataList;
        public UserData newUser;
        public JoinRoomResponseType joinRoomResponseType;
    }
    
    public class ExitRoomReq : BaseRequestData
    {
        public string roomName;
        public UserData userData;
    }

    public class ExitRoomRes : BaseResponseData
    {
        public bool isSuccess;
        public UserData exitRoomUserData;
    }
}
