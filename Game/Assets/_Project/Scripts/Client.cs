using System;
using System.Collections.Generic;
using System.Net;
using Hub.Hooks;
using UnityEngine;

namespace _Project.Scripts {
    [DefaultExecutionOrder(100)]
    public class Client : MonoBehaviour {
        public static Queue<Message> Messages = new();

        public static Action OnConnect = delegate { };
        public static Action<Guid> OnPlayerConnected = delegate { };
        public static Action<Guid, Vector3> OnPositionReceived = delegate { };
        public static Action<Guid, string> OnChat = delegate { };

        public static Guid ClientId, ChatId;

        [SerializeField] PositionClient positionClient;
        [SerializeField] TextClient textClient;

        HashSet<Guid> otherClients = new();

        public void Initialize(IPAddress address) {
            textClient.Initialize(address, 8888);
            positionClient.Initialize(address, 8889);
        }

        void OnEnable() {
            textClient.OnConnect += OnConnect;
        }

        void OnDisable() {
            textClient.OnConnect -= OnConnect;
        }
        
        public void Send(string message) {
            textClient.Send(message);
        }
        
        void Update() {
            while(Messages.Count > 0) {
                var message = Messages.Dequeue();
                switch(message.Header) {
                    case MessageType.Position: {
                        var positionTuple = message.DeserializePosition();
                        var position = new Vector3(positionTuple.Item1, positionTuple.Item2, positionTuple.Item3);
                        if (otherClients.Add(message.SenderId)) {
                            OnPlayerConnected.Invoke(message.SenderId);
                        }
                        OnPositionReceived.Invoke(message.SenderId, position);
                    }
                        break;

                    case MessageType.Text: {
                        var text = message.DeserializeText();
                        OnChat.Invoke(message.SenderId, text);
                    }
                        break;
                }
            }
        }
    }
}
