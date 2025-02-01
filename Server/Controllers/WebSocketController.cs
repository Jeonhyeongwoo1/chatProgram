using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using Chatting.Enum;
using Chatting.RPC;
using Chatting.RPCHandler;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

public class UserData
{
    public string userName;
    public WebSocket webSocket;
}

public class RoomData
{
    public bool IsMax => MaxCount == userDataList.Count;
    public bool IsEmpty => userDataList.Count == 0;
    public string RoomName => roomName;

    public const int MaxCount = 3;
    public List<UserData> userDataList = new List<UserData>(MaxCount);
    public UserData host;
    private string roomName;

    public void CreateRoom(WebSocket user, UserData userData, string roomName)
    {
        userData.webSocket = user;
        userDataList.Add(userData);
        host = userData;
        this.roomName = roomName;
    }

    public void AddUser(WebSocket user, UserData userData)
    {
        userData.webSocket = user;
        userDataList.Add(userData);
    }

    public void RemoveUser(WebSocket user, string userName)
    {
        var data = userDataList.Find(v=> v.userName == userName);
        if(data != null)
        {
            userDataList.Remove(data);
        }

        if(host.userName == userName && userDataList.Count > 0)
        {
            Random rand = new Random();
            int select = rand.Next(0, userDataList.Count);
            try
            {
                host = userDataList[select]; 
            }catch(Exception e)
            {
                host = null;
            }
        }
    }
}

namespace Chatting.Controllers
{
    [Route("ws")]
    public class WebSocketController : ControllerBase
    {
        private readonly static List<WebSocket> _webSocketList = new List<WebSocket>();
        private readonly static ConcurrentQueue<(WebSocketResponseData data, WebSocket sender)> _messageQueue = new(); //멀티쓰레드
        private readonly static Dictionary<WebSocket, string> _roomWebSocketDict = new Dictionary<WebSocket, string>();
        private readonly static Dictionary<string, RoomData> _roomDataDict = new Dictionary<string, RoomData>();
        private RpcHandler _rpcHandler;
        private CancellationTokenSource _webSocketCts = new CancellationTokenSource();
        private Task _handleMessageTask;
        private Task _pingTask;
        public WebSocketController ()
        {
            _rpcHandler = new RpcHandler(this);
            _handleMessageTask = Task.Run(HandleMessage);
            _pingTask = Task.Run(StartPingpongLoopTask);
        }

        [HttpGet]
        public async Task Get()
        {
            if (HttpContext.WebSockets.IsWebSocketRequest)
            {
                WebSocket websocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
                await HandleWebsocket(websocket);
            }
        }

        public static void EnqueueMessage(WebSocketResponseData data, WebSocket sender)
        {
            _messageQueue.Enqueue((data, sender));
        }

        private async Task HandleMessage()
        {
            while (_webSocketCts.IsCancellationRequested == false)
            {
                while (_messageQueue.TryDequeue(out var item))
                {
                    var jsonString = JsonConvert.SerializeObject(item.data);
                    var serverBuffer = Encoding.UTF8.GetBytes(jsonString);

                    await BroadcastMessage(serverBuffer, item.data.webSocketMessageType, item.sender);
                }

                await Task.Delay(10);
            }
        }

        private void DisposeWebSocket()
        {
            if(_webSocketCts == null) return;

            _handleMessageTask.Dispose();
            _webSocketCts.Cancel();
            _webSocketCts.Dispose();
        }

        private async Task HandleWebsocket(WebSocket webSocket)
        {
            var buffer = new byte[1024 * 4];

            if (!_webSocketList.Contains(webSocket))
            {
                _webSocketList.Add(webSocket);
            }

            try
            {
                while (webSocket.State == WebSocketState.Open)
                {
                    var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), _webSocketCts.Token);

                    if (result.MessageType == WebSocketMessageType.Text || result.MessageType == WebSocketMessageType.Binary)
                    {
                        var clientMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);

                        var requestData = JsonConvert.DeserializeObject<WebSocketRequestData>(clientMessage);
                        if(requestData != null)
                        {
                            if (requestData.eventType == ChatEventType.PingPong.ToString())
                            {
                                RecievedPoing(webSocket);
                            }
                            else
                            {
                                _rpcHandler.HandlePacket(requestData, webSocket);
                            }
                        }
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        DisposeWebSocket(webSocket);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            finally
            {
                DisposeWebSocket();
            }
        }

        private async void DisposeWebSocket(WebSocket webSocket)
        {
            _webSocketList.Remove(webSocket);
            if (_roomWebSocketDict.TryGetValue(webSocket, out string roomName))
            {
                if (_roomDataDict.TryGetValue(roomName, out var roomData))
                {
                    var userData = roomData.userDataList.Find(v => v.webSocket == webSocket);
                    if (userData != null)
                    {
                        roomData.userDataList.Remove(userData);
                        if (roomData.userDataList.Count == 0)
                        {
                            _roomDataDict.Remove(roomName);
                        }
                    }
                }

                _roomWebSocketDict.Remove(webSocket);
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Connection closed", _webSocketCts.Token);
            }
        }

        private async Task BroadcastMessage(byte[] messageBuffer, WebSocketMessageType messageType, WebSocket sender)
        {
            //Room이 있을 경우
            if (_roomWebSocketDict.TryGetValue(sender, out string roomName))
            {
                if(!_roomDataDict.TryGetValue(roomName, out var roomData))
                {
                    Console.WriteLine($"room is numm " + roomName);
                    return;
                }

                foreach (var item in roomData.userDataList)
                {
                    try
                    {
                        await item.webSocket.SendAsync(new ArraySegment<byte>(messageBuffer), messageType, true, _webSocketCts.Token);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error sending message to client: {ex.Message}");
                    }
                }
            }
            else
            {
                //자기자신에게만
                if (sender.State == WebSocketState.Open)
                {
                    try
                    {
                        await sender.SendAsync(new ArraySegment<byte>(messageBuffer), messageType, true, _webSocketCts.Token);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error sending message to client: {ex.Message}");
                    }
                }
            }
        }

        private int disconnectTime = 8;
        private int pingTime = 5000;
        private Dictionary<WebSocket, DateTime> _lastPongReceivedDict = new();
        private async Task StartPingpongLoopTask()
        {
            while(_webSocketCts.IsCancellationRequested == false)
            {
                DateTime now = DateTime.UtcNow;
                WebSocketResponseData resData = new WebSocketResponseData();
                resData.eventType = ChatEventType.PingPong.ToString();
                var jsonString = JsonConvert.SerializeObject(resData);
                var message = Encoding.UTF8.GetBytes(jsonString);

                foreach (var item in _webSocketList)
                {
                    try
                    {
                        await item.SendAsync(new ArraySegment<byte>(message), WebSocketMessageType.Text, true, _webSocketCts.Token);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error sending message to client: {ex.Message}");
                    }

                    if(_lastPongReceivedDict.TryGetValue(item, out var dateTime))
                    {
                        if((dateTime - now).TotalSeconds > disconnectTime)
                        {
                            DisposeWebSocket(item);
                        }
                    }
                }

                await Task.Delay(pingTime);
            }
        }

        private void RecievedPoing(WebSocket webSocket)
        {
            if(_lastPongReceivedDict.TryGetValue(webSocket, out var dateTime))
            {
                _lastPongReceivedDict[webSocket] = DateTime.Now;
            }
        }


        #region Room

        [NonAction]
        public bool IsExistRoom(string roomName)
        {
            return _roomDataDict.ContainsKey(roomName);
        }

        [NonAction]
        public bool TryCreateRoom(string roomName, UserData userData, WebSocket master)
        {
            if (IsExistRoom(roomName))
            {
                return false;
            }

            RoomData roomData = new RoomData();
            roomData.CreateRoom(master, userData, roomName);
            _roomDataDict.Add(roomName, roomData);  
            _roomWebSocketDict.Add(master, roomName);
            return true;
        }

        [NonAction]
        public bool IsInRoom(WebSocket sender)
        {
            if(!_roomWebSocketDict.TryGetValue(sender, out var roomName))
            {
                return false;
            }

            if(!_roomDataDict.TryGetValue(roomName, out var roomData))
            {
                return false;
            }

            return true;
        }

        [NonAction]
        public JoinRoomResponseType TryJoinRoom(string roomName, UserData userData, WebSocket user)
        {
            if(!_roomDataDict.ContainsKey(roomName))
            {
                return JoinRoomResponseType.NotExistRoom;
            }

            RoomData roomData = _roomDataDict[roomName];
            if(roomData.IsMax)
            {
                return JoinRoomResponseType.MaxCount;
            }

            roomData.AddUser(user, userData);
            _roomWebSocketDict.Add(user, roomName);

            return JoinRoomResponseType.Success;
        }

        [NonAction]
        public List<UserData>? GetUserListInRoom(string roomName)
        {
            if(_roomDataDict.TryGetValue(roomName, out RoomData roomData))
            {
                return roomData.userDataList;
            }

            return null;
        }

        public UserData GetHostUserData(string roomName)
        {
            if (_roomDataDict.TryGetValue(roomName, out RoomData roomData))
            {
                return roomData.host;
            }

            return null;
        }

        [NonAction]
        public bool TryExitRooom(string roomName, UserData userData, WebSocket user)
        {
            if(!_roomDataDict.TryGetValue(roomName, out RoomData roomData))
            {
                return false;       
            }

            if(!_roomWebSocketDict.ContainsKey(user))
            {
                return false;
            }

            _roomWebSocketDict.Remove(user);
            roomData.RemoveUser(user, userData.userName);
            Console.WriteLine(roomData.IsEmpty);
            if(roomData.IsEmpty)
            {
                _roomDataDict.Remove(roomName);
            }

            return true;
        }

        #endregion
    }
}
