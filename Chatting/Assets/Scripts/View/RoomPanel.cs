using System;
using UnityEngine;
using UnityEngine.UI;

namespace DefaultNamespace.View
{
    public class RoomPanel : MonoBehaviour
    {
        /*
         *  1. 방 생성
         *  2. 방 입장
         *  3. 방 리스트 조회
         * 
         */

        [SerializeField] private Button _createRoomButton;
        [SerializeField] private Button _joinRoomButton;
        [SerializeField] private CreateRoomPopup _createRoomPopup;
        [SerializeField] private JoinRoomPopup _joinRoomPopup;
        
        private void Start()
        {
            _createRoomButton.onClick.AddListener(()=> _createRoomPopup.ShowPopup());
            _joinRoomButton.onClick.AddListener(()=> _joinRoomPopup.ShowPopup());
        }

        public void Hide()
        {
            _createRoomPopup.HidePopup();
            _joinRoomPopup.HidePopup();
            gameObject.SetActive(false);
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }
    }
}