using System;
using TMPro;
using UnityEngine;
namespace _Project.Scripts {
    public class ChatManager : MonoBehaviour {
        [SerializeField] RectTransform chatMessages;
        [SerializeField] TextMeshProUGUI chatMessagePrefab;
        [SerializeField] Client client;

        void OnEnable() {
            Client.OnChat += OnReceive;
        }

        void OnDisable() {
            Client.OnChat -= OnReceive;
        }
        public void Send(string message) {
            Debug.Log($"Sending message: {message}");
            client.Send(message);
        }

        public void OnReceive(Guid id, string text) {
            var message = Instantiate(chatMessagePrefab, chatMessages);
            message.text = text;
            Destroy(message.gameObject, 5);
        }
    }
}
