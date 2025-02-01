using System;
using System.Collections.Generic;
using System.Text;
using Data;
using DefaultNamespace;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class ChattingView : MonoBehaviour
{
    enum ChatType
    {
        Whisper,
        All,
    }

    private ChatType chatType
    {
        set
        {
            _chatType = value;
            _inputMessageBuilder.Clear();
            switch (value)
            {
                case ChatType.Whisper:
                    _allTextInput.text = _whisperPrefix;
                    _messagePrefix = _whisperPrefix;
                    _whisperTextInput.gameObject.SetActive(true);
                    _allTextInput.gameObject.SetActive(false);
                    break;
                case ChatType.All:
                    _messagePrefix = "";
                    _allTextInput.text = "";
                    _whisperTextInput.gameObject.SetActive(false);
                    _allTextInput.gameObject.SetActive(true);
                    break;
            }
        }
    }
    
    [SerializeField] private Text _roomName;
    [SerializeField] private ScrollRect _scrollRect;
    [SerializeField] private ChatPanel _chatPanelPrefab;
    [SerializeField] private Button _sendButton;
    [FormerlySerializedAs("_textInput")] [SerializeField] private InputField _allTextInput;
    [SerializeField] private InputField _whisperTextInput;
    [SerializeField] private GridLayoutGroup _gridLayoutGroup;
    [SerializeField] private Button _exitRoomButton;
    [SerializeField] private Dropdown _chatTypeDropdown;
    
    private ChatType _chatType = ChatType.All;
    private List<ChatPanel> _chatPanels = new List<ChatPanel>();
    private int _count = 10;
    private bool _isInitialized;
    private string _whisperPrefix = "who : ";
    private string _messagePrefix;
    private StringBuilder _inputMessageBuilder = new StringBuilder();

    private void Awake()
    {
        Initialize();
    }

    private void OnExitRoom()
    {
        ChatController.Instance.ExitRoom();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            ChangeChatType(ChatType.Whisper);
        }
        
        if (Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            OnSendMessage(_inputMessageBuilder.ToString());
        }
    }

    private void ChangeChatType(ChatType chatType)
    {
        this.chatType = chatType;
    }
 
    public void Initialize()
    {
        if (_isInitialized)
        {
            return;
        }
        
        _isInitialized = true;
        for (int i = 0; i < _count; i++)
        {
            var chatPanel = Instantiate(_chatPanelPrefab, _scrollRect.content);
            _chatPanels.Add(chatPanel);
            
            chatPanel.gameObject.SetActive(false);
            chatPanel.name = i.ToString();
        }
        
        _allTextInput.onValueChanged.AddListener(OnTextChanged);
        _whisperTextInput.onValueChanged.AddListener(OnTextChanged);
        _exitRoomButton.onClick.AddListener(OnExitRoom);
        _chatTypeDropdown.onValueChanged.AddListener(OnChangeChatTypeDropdown);
        // _textInput.onSubmit.AddListener(OnSendMessage);
    }

    private void OnTextChanged(string userInput)
    {
        if (string.IsNullOrEmpty(userInput))
        {
            return;
        }

        _inputMessageBuilder.Append(_allTextInput.text);
    }
    
    private void OnChangeChatTypeDropdown(int index)
    {
        string type = _chatTypeDropdown.options[index].text;
        ChatType eventType = (ChatType)Enum.Parse(typeof(ChatType), type);
        ChangeChatType(eventType);
    }

    public void Show(string roomName)
    {
        gameObject.SetActive(true);
        _roomName.text = roomName;
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private void OnSendMessage(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        _allTextInput.text = "";
        WebSocketRequestData data = new WebSocketRequestData
        {
            eventType = ChatEventType.SendMessage.ToString()
        };
        
        ChatDataReq chatDataReq = new ChatDataReq
        {
            userName = ChatController.Instance.UserName,
            message = _messagePrefix + text
        };
        
        string message = JsonUtility.ToJson(chatDataReq);
        data.jsonString = message;
        WebSocketController.Instance.EnqueueMessage(data);
    }

    private void OnWhisperMessage(string text, string reciverUserName)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        _allTextInput.text = "";
        WebSocketRequestData data = new WebSocketRequestData
        {
            eventType = ChatEventType.SendMessage.ToString()
        };

        WhisperMessageReq chatDataReq = new WhisperMessageReq
        {
            senderUserName = ChatController.Instance.UserName,
            receiverUserName = reciverUserName,
            message = text
        };
        
        string message = JsonUtility.ToJson(chatDataReq);
        data.jsonString = message;
        WebSocketController.Instance.EnqueueMessage(data);
    }

    public void Message(string message)
    {
        // 현재 활성화된 패널의 개수
        int count = _chatPanels.FindAll(x => x.gameObject.activeSelf).Count;

        int minCount = 6;
        float cellHeight = _gridLayoutGroup.cellSize.y + _gridLayoutGroup.spacing.y;
        float y = minCount * cellHeight; // 최소 높이

        if (count > minCount)
        {
            y = count * cellHeight;
        }

        _scrollRect.content.sizeDelta = new Vector2(_scrollRect.content.sizeDelta.x, y);

        ChatPanel panel = _chatPanels.Find(x => !x.gameObject.activeSelf);
        if (panel == null)
        {
            panel = _scrollRect.content.GetChild(0).GetComponent<ChatPanel>();
            panel.UpdateUI(message);
        }
        else
        {
            panel.UpdateUI(message);
        }

        panel.transform.SetAsLastSibling();
        Canvas.ForceUpdateCanvases(); // UI 강제 업데이트
        _scrollRect.verticalNormalizedPosition = 0f; // 맨 아래로 스크롤
    }
}
