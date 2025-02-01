using System.Net.WebSockets;
using Chatting.Controllers;
using Newtonsoft.Json;

namespace Chatting.RPC
{
    public class Chat
    {
        private WebSocketController _webSocketController;

        public Chat(WebSocketController webSocketController)
        {
            _webSocketController = webSocketController;
        }

        [RpcMethod]
        public void SendMessage(object packetData, WebSocket sender)
        {
            WebSocketRequestData packet = (WebSocketRequestData)packetData;

            var chatData = JsonConvert.DeserializeObject<ChatDataReq>(packet.jsonString);

            if (!_webSocketController.IsInRoom(sender))
            {
                return;  
            }
            
            ChatDataRes data = new ChatDataRes();
            data.message = chatData.message;
            data.userName = chatData.userName;

            WebSocketResponseData responseData = new WebSocketResponseData();
            responseData.eventType = $"{nameof(SendMessage)}";
            responseData.jsonString = JsonConvert.SerializeObject(data);
            WebSocketController.EnqueueMessage(responseData, sender);
        }
    }
}
