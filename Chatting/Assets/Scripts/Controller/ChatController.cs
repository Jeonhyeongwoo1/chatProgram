using System;
using System.Collections.Generic;
using Data;
using DefaultNamespace.View;
using Newtonsoft.Json;
using UnityEngine;

namespace DefaultNamespace
{
    
    public class RoomData
    {
        public string roomName;
        public List<UserData> userDataList;
    }
    
    public class ChatController : MonoBehaviour
    {
        private static ChatController _instance;

        public static ChatController Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<ChatController>();
                }

                return _instance;
            }
        }
        
        [SerializeField] private ChattingView _chatView;
        [SerializeField] private RoomPanel _roomPanel;

        public string UserName => _userData.userName;
        
        private RoomData _roomData;
        private UserData _userData;
        
        private void Awake()
        {
            EventManager.Instance.AddListener(ChatEventType.SendMessage.ToString(), (key, o) => OnMessage((string)o));
            EventManager.Instance.AddListener(ChatEventType.CreateRoom.ToString(), (key, o) => OnCreateRoom((string)o));
            EventManager.Instance.AddListener(ChatEventType.JoinRoom.ToString(), (key, o) => OnJoinRoom((string)o));
            EventManager.Instance.AddListener(ChatEventType.ExitRoom.ToString(), (key, o) => OnExitRoom((string)o));
            EventManager.Instance.AddListener(ChatEventType.WhisperMessage.ToString(), (key, o) => OnWhisperMessage((string)o));
            _userData = new UserData() { userName = GetHashCode().ToString() };
        }

        public void CreateRoom(string roomName)
        {
            CreateRoomReq roomRequestData = new CreateRoomReq
            {
                roomName = roomName,
                userData = _userData
            };
            
            WebSocketRequestData requestData = new WebSocketRequestData
            {
                eventType = ChatEventType.CreateRoom.ToString(),
                jsonString = JsonConvert.SerializeObject(roomRequestData)
            };
            
            Debug.Log("roomName : " + roomName);
            WebSocketController.Instance.EnqueueMessage(requestData);
        }

        public void JoinRoom(string roomName)
        {
            JoinRoomReq roomRequestData = new JoinRoomReq()
            {
                roomName = roomName,
                userData = _userData
            };
            
            WebSocketRequestData requestData = new WebSocketRequestData
            {
                eventType = ChatEventType.JoinRoom.ToString(),
                jsonString = JsonConvert.SerializeObject(roomRequestData)
            };
            
            Debug.Log("roomName : " + roomName);
            WebSocketController.Instance.EnqueueMessage(requestData);
        }

        public void ExitRoom()
        {
            string roomName = _roomData.roomName;

            ExitRoomReq exitRoomReq = new ExitRoomReq();
            exitRoomReq.roomName = roomName;
            exitRoomReq.userData = _userData;
            
            WebSocketRequestData requestData = new WebSocketRequestData
            {
                eventType = ChatEventType.ExitRoom.ToString(),
                jsonString = JsonConvert.SerializeObject(exitRoomReq)
            };
            
            Debug.Log("roomName : " + roomName);
            WebSocketController.Instance.EnqueueMessage(requestData);
        }

        private void OnExitRoom(string jsonString)
        {
            var response = JsonConvert.DeserializeObject<ExitRoomRes>(jsonString);
            if (response.isSuccess)
            {
                if (response.exitRoomUserData.userName == _userData.userName)
                {
                    _roomPanel.Show();
                    _chatView.Hide();
                    _roomData = null;
                }
                else
                {
                    string message = $"Exit room user {response.exitRoomUserData.userName}";
                    _chatView.Message(message);
                }
            }
            else
            {
                Debug.Log("Failed exit room");
            }
        }

        private void OnJoinRoom(string jsonString)
        {
            var response = JsonConvert.DeserializeObject<JoinRoomRes>(jsonString);

            switch (response.joinRoomResponseType)
            {
                case JoinRoomResponseType.Success:
                    _roomPanel.Hide();
                    _chatView.Initialize();
                    _chatView.Show(response.roomName);

                    _roomData = new RoomData();
                    _roomData.roomName = response.roomName;
                    _roomData.userDataList ??= new List<UserData>();
                    _roomData.userDataList.AddRange(response.userDataList);

                    if (response.newUser != null && response.newUser.userName != _userData.userName)
                    {
                        string message = $"joined new user : {response.newUser.userName}"; 
                        _chatView.Message(message);
                    }
                    
                    foreach (UserData userData in response.userDataList)
                    {
                        Debug.Log(userData.userName);
                    }
                    break;
                case JoinRoomResponseType.NotExistRoom:
                    Debug.Log("Not exist room");
                    break;
                case JoinRoomResponseType.Fail:
                    break;
                case JoinRoomResponseType.MaxCount:
                    Debug.Log("room is full");
                    break;
            }
        }

        private void OnCreateRoom(string jsonString)
        {
            var response = JsonConvert.DeserializeObject<CreateRoomRes>(jsonString);

            if (response.createRoomResponseType == CreateRoomResponseType.Success)
            {
                _roomPanel.Hide();
                _chatView.Initialize();
                _chatView.Show(response.roomName);

                _roomData = new RoomData();
                _roomData.roomName = response.roomName;
                _roomData.userDataList ??= new List<UserData>();
                _roomData.userDataList.Add(response.userData);
            }
            else if (response.createRoomResponseType == CreateRoomResponseType.IsExist)
            {
                Debug.Log("exist room" + response.roomName);
            }
            else
            {
                Debug.Log("failed");
            }
        }

        private void OnWhisperMessage(string jsonString)
        {
            
        }

        public void OnMessage(string jsonString)
        {
            var response = JsonConvert.DeserializeObject<ChatDataRes>(jsonString);
            _chatView.Message(response.message);
        }
    }
}