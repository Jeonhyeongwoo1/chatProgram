using UnityEngine;
using UnityEngine.UI;

namespace DefaultNamespace.View
{
    public class JoinRoomPopup : MonoBehaviour
    {
        [SerializeField] private Button _joinRoomButton;
        [SerializeField] private InputField _roomNameInputField;
        [SerializeField] private Button _closeButton;

        private void Start()
        {
            _joinRoomButton.onClick.AddListener(OnJoinRoom);
            _closeButton.onClick.AddListener(HidePopup);
        }
        private void OnJoinRoom()
        {
            string roomName = _roomNameInputField.text;
            if (string.IsNullOrEmpty(roomName))
            {
                //alert
                return;
            }
            
            ChatController.Instance.JoinRoom(roomName);
            // HidePopup();
        }

        public void ShowPopup()
        {
            gameObject.SetActive(true);
        }
        
        public void HidePopup()
        {
            gameObject.SetActive(false);
        }
    }
}