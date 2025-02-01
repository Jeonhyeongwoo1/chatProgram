using System;
using UnityEngine;
using UnityEngine.UI;

namespace DefaultNamespace.View
{
    public class CreateRoomPopup : MonoBehaviour
    {
        
        [SerializeField] private Button _createRoomButton;
        [SerializeField] private InputField _roomNameInputField;
        [SerializeField] private Button _closeButton;

        private void Start()
        {
            _createRoomButton.onClick.AddListener(OnCreateRoom);
            _closeButton.onClick.AddListener(HidePopup);
        }
        private void OnCreateRoom()
        {
            string roomName = _roomNameInputField.text;
            if (string.IsNullOrEmpty(roomName))
            {
                //alert
                return;
            }
            
            ChatController.Instance.CreateRoom(roomName);
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