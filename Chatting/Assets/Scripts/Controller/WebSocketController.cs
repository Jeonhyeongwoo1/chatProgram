using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Data;
using DefaultNamespace;
using Newtonsoft.Json;
using UnityEngine;

public class WebSocketController : MonoBehaviour
{
    private static WebSocketController _instance;

    public static WebSocketController Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<WebSocketController>();
            }

            return _instance;
        }
    }

    private ClientWebSocket _ws;
    private CancellationTokenSource _socketCts;
    private readonly Queue<WebSocketRequestData> _sendQueue = new();
    private readonly Queue<string> _receiveQueue = new();
    private Task _webSocketTask;
    
    private void Start()
    {
        _ws = new ClientWebSocket();
        _socketCts = new CancellationTokenSource();
        ConnectSocket();
        EventManager.Instance.AddListener(ChatEventType.PingPong.ToString(),OnRecievedPong);
    }

    private async void ConnectSocket()
    {
        Uri uri = new Uri("ws://127.0.0.1:8080/ws");
        try
        {
            await _ws.ConnectAsync(uri, _socketCts.Token);

            if (_ws.State == WebSocketState.Open)
            {
                Debug.Log("WebSocket connected successfully.");
                _webSocketTask = Task.WhenAll(SendTaskAsync(), ReceiveTaskAsync(), ReconnectTaskAsync());
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to connect WebSocket: {ex.Message}");
            Dispose();
        }
    }
    
    private async Task ReconnectTaskAsync()
    {
        while (true)
        {
            if (_ws == null || _ws.State == WebSocketState.Closed)
            {
                _socketCts.Cancel();
                _socketCts = null;
                _socketCts = new CancellationTokenSource();
                _ws = new ClientWebSocket();
                
                ConnectSocket();
            }
            
            await Task.Delay(5000);
        }
    }

    public void EnqueueMessage(WebSocketRequestData data)
    {
        lock (_sendQueue)
        {
            _sendQueue.Enqueue(data);
        }
    }

    private async Task SendTaskAsync()
    {
        try
        {
            while (!_socketCts.IsCancellationRequested)
            {
                while (_sendQueue.Count > 0)
                {
                    WebSocketRequestData data;

                    lock (_sendQueue)
                    {
                        if (_ws.State != WebSocketState.Open)
                        {
                            break;
                        }
                        
                        data = _sendQueue.Peek();
                    }

                    string json = JsonUtility.ToJson(data);
                    byte[] buffer = Encoding.UTF8.GetBytes(json);

                    try
                    {
                        await _ws.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, _socketCts.Token);
                        Debug.Log($"Message sent: {json}");
                        _sendQueue.Dequeue();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Error sending message: {ex.Message}");
                        
                    }
                }

                await Task.Delay(10); // Prevents tight looping
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"SendTask error: {ex.Message}");
            Dispose();
        }
    }

    private async Task ReceiveTaskAsync()
    {
        try
        {
            byte[] buffer = new byte[1024 * 4];

            while (!_socketCts.IsCancellationRequested)
            {
                WebSocketReceiveResult result;

                try
                {
                    result = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer), _socketCts.Token);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error receiving message: {ex.Message}");
                    Dispose();
                    return;
                }

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    Debug.Log("WebSocket connection closed by server.");
                    Dispose();
                    return;
                }

                string receivedMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);
                Debug.Log($"Message received: {receivedMessage}");

                var responseData = JsonConvert.DeserializeObject<WebSocketResponseData>(receivedMessage);
                EventManager.Instance.Notify(responseData.eventType, responseData.jsonString);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"ReceiveTask error: {ex.Message}");
            Dispose();
        }
    }

    private void OnRecievedPong(string key, object jsonString)
    {
        WebSocketRequestData requestData = new();
        requestData.eventType = ChatEventType.PingPong.ToString();
        
        EnqueueMessage(requestData);
    }
    
    private void Dispose()
    {
        try
        {
            _ws?.Dispose();
            _socketCts?.Cancel();
            _socketCts?.Dispose();
            Debug.Log("WebSocket disposed.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Dispose error: {ex.Message}");
        }
    }
}
