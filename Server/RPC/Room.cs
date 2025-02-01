using System.Net.WebSockets;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Chatting.Enum;
using Chatting.Controllers;

namespace Chatting.RPC
{
    public class Room
    {
        private WebSocketController _webSocketController;
      
        public Room(WebSocketController webSocketController)
        {
            _webSocketController = webSocketController; 
        }

        [RpcMethod]
        public void JoinRoom(object packetData, WebSocket sender)
        {
            WebSocketRequestData packet = (WebSocketRequestData)packetData;
            var roomData = JsonConvert.DeserializeObject<CreateRoomReq>(packet.jsonString);

            WebSocketResponseData responseData = new WebSocketResponseData();
            responseData.eventType = ChatEventType.JoinRoom.ToString();

            JoinRoomResponseType roomResponseType = _webSocketController.TryJoinRoom(roomData.roomName, roomData.userData, sender);

            JoinRoomRes roomRes = new JoinRoomRes
            {
                roomName = roomData.roomName,
                joinRoomResponseType = roomResponseType,
                userDataList = _webSocketController.GetUserListInRoom(roomData.roomName),
                newUser = roomData.userData
            };

            responseData.jsonString = JsonConvert.SerializeObject(roomRes);
            WebSocketController.EnqueueMessage(responseData, sender);
        }

        [RpcMethod]
        public void CreateRoom(object packetData, WebSocket sender)
        {
            WebSocketRequestData packet = (WebSocketRequestData)packetData;
            var roomData = JsonConvert.DeserializeObject<CreateRoomReq>(packet.jsonString);

            WebSocketResponseData responseData = new WebSocketResponseData();
            responseData.eventType = ChatEventType.CreateRoom.ToString();
            
            //방이 있을 경우
            if (_webSocketController.IsExistRoom(roomData.roomName))
            {
                CreateRoomRes roomRes = new CreateRoomRes
                {
                    roomName = roomData.roomName,
                    userData = roomData.userData,
                    createRoomResponseType = CreateRoomResponseType.IsExist
                };

                responseData.jsonString = JsonConvert.SerializeObject(roomRes);
                WebSocketController.EnqueueMessage(responseData, sender);
            }
            else
            {
                CreateRoomRes roomRes = new CreateRoomRes
                {
                    roomName = roomData.roomName,
                    userData = roomData.userData,
                    createRoomResponseType = CreateRoomResponseType.Success
                };

                bool isSuccess = _webSocketController.TryCreateRoom(roomData.roomName, roomData.userData, sender);
                if (!isSuccess)
                {
                    roomRes.createRoomResponseType = CreateRoomResponseType.Fail;
                }

                responseData.jsonString = JsonConvert.SerializeObject(roomRes);
                WebSocketController.EnqueueMessage(responseData, sender);
            }
        }

        [RpcMethod]
        public void ExitRoom(object packetData, WebSocket sender)
        {
            WebSocketRequestData packet = (WebSocketRequestData)packetData;
            var roomData = JsonConvert.DeserializeObject<CreateRoomReq>(packet.jsonString);

            WebSocketResponseData responseData = new WebSocketResponseData();
            responseData.eventType = ChatEventType.ExitRoom.ToString();

            bool isSuccess = _webSocketController.TryExitRooom(roomData.roomName, roomData.userData, sender);
            ExitRoomRes res = new ExitRoomRes();
            res.isSuccess = isSuccess;
            res.exitRoomUserData = roomData.userData;
            responseData.jsonString = JsonConvert.SerializeObject(res);
            WebSocketController.EnqueueMessage(responseData, sender);
            UserData hostUserData = _webSocketController.GetHostUserData(roomData.roomName);

            //나머지 룸 인원에게 알림
            if (hostUserData != null && hostUserData.userName != roomData.userData.userName)
            {
                WebSocketController.EnqueueMessage(responseData, hostUserData.webSocket);
            }
        }
    }
}
