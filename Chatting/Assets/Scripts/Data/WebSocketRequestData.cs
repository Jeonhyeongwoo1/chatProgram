using System;
using System.Collections.Generic;
using UnityEngine.Serialization;

namespace Data
{
    [Serializable]
    public class WebSocketRequestData
    {
        public string eventType;
        public string jsonString;
    }
    
    [Serializable]
    public class WebSocketResponseData
    {
        public string eventType;
        public string jsonString;
    }

    [Serializable]
    public class BaseRequestData
    {
        
    }

    [Serializable]
    public class BaseResponseData
    {
        
    }

    [Serializable]
    public class ChatDataReq : BaseRequestData
    {
        public string userName;
        public string message;
    }

    [Serializable]
    public class ChatDataRes : BaseResponseData
    {
        public string userName;
        public string message;
    }

    [Serializable]
    public class WhisperMessageReq : BaseRequestData
    {
        public string senderUserName;
        public string receiverUserName;
        public string message;
    }
    
    [Serializable]
    public class WhisperMessageRes : BaseResponseData
    {
        public string senderUserName;
        public string receiverUserName;
        public string message;
    }

    [Serializable]
    public class CreateRoomReq : BaseRequestData
    {
        public UserData userData;
        public string roomName;
    }

    [Serializable]
    public class CreateRoomRes : BaseResponseData
    {
        public UserData userData;
        public string roomName;
        public CreateRoomResponseType createRoomResponseType;
    }
    
    [Serializable]
    public class JoinRoomReq : BaseRequestData
    {
        public UserData userData;
        public string roomName;
    }

    [Serializable]
    public class JoinRoomRes : BaseResponseData
    {
        public string roomName;
        public List<UserData> userDataList;
        public UserData newUser;
        public JoinRoomResponseType joinRoomResponseType;
    }
    
    [Serializable]
    public class ExitRoomReq : BaseRequestData
    {
        public string roomName;
        public UserData userData;
    }

    [Serializable]
    public class ExitRoomRes : BaseResponseData
    {
        public bool isSuccess;
        public UserData exitRoomUserData;
    }
}