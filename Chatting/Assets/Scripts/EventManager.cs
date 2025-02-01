using System.Collections.Generic;
using UnityEngine;

namespace DefaultNamespace
{
    public delegate void OnEvent(string key, object o);
    public class EventManager : MonoBehaviour
    {
        private static EventManager _instance;

        public static EventManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<EventManager>();
                }

                return _instance;
            }
        }

        private Dictionary<string, OnEvent> _events = new Dictionary<string, OnEvent>();

        public void AddListener(string key, OnEvent onEvent)
        {
            _events.Add(key, onEvent);
        }

        public void RemoveListener(string key, OnEvent onEvent)
        {
            _events.Remove(key);
        }

        public void Notify(string key, object o)
        {
            if (_events.TryGetValue(key, out var _event))
            {
                _event.Invoke(key, o);
            }
            
        }
    }
}