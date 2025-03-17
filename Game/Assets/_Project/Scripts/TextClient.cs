using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Hub.Hooks;
using UnityEngine;
namespace _Project.Scripts {
    public class TextClient : MonoBehaviour {
        const int BUFFER_SIZE = 1024;

        public Action OnConnect = delegate { };

        byte[] buffer;
        Socket client;

        Queue<Message> messages = new();

        public void Initialize(IPAddress address, int port) {
            Debug.Log("Starting Client...");

            buffer = new byte[BUFFER_SIZE];
            client = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            client.Connect(address, port);
            Debug.Log("Connected to the server");

            OnConnect.Invoke();

            // Start receiving data from the server
            client.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, OnReceive, client);
            _ = SendLoop();
        }
        async Task SendLoop() {
            for (;;) {
                while(messages.Count > 0) {
                    var message = messages.Dequeue();
                    var bytes = message.SerializeMessage();
                    Debug.Log("Attempting Send...");
                    client.BeginSend(bytes, 0, bytes.Length, SocketFlags.None, OnSend, client);
                }
                await Awaitable.WaitForSecondsAsync(0.1f);
            }
        }

        public void Send(string input) {
            if (Client.ChatId == Guid.Empty) {
                Debug.Log("The server has not assigned you an ID yet.");
                return;
            }

            var message = MessageUtils.CreateText(Client.ChatId, input);

            messages.Enqueue(message);
        }

        void OnSend(IAsyncResult result) {
            try {
                if (result.AsyncState is Socket server) {
                    server.EndSend(result);
                }
            } catch (Exception e) {
                Debug.LogError(e);
            }
        }

        void OnReceive(IAsyncResult result) {
            var server = (Socket)result.AsyncState!;
            var bytesReceived = server.EndReceive(result);

            var bytes = new byte[bytesReceived];
            Buffer.BlockCopy(buffer, 0, bytes, 0, bytesReceived);

            var message = bytes.DeserializeMessage();

            switch(message.Header) {
                case MessageType.Text: {
                    var text = message.DeserializeText();
                    Debug.Log($"{message}: {text}");
                    Client.Messages.Enqueue(message);
                }
                    break;
                case MessageType.Registration: {
                    if (Client.ChatId != Guid.Empty) {
                        server.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, OnReceive, server);
                        return;
                    }
                    Client.ChatId = message.DeserializeGuid();
                    Debug.Log($"Registered with TextID: {Client.ChatId}");
                    Client.Messages.Enqueue(message);
                }
                    break;
                case MessageType.Position:
                case MessageType.Error:
                default: break;
            }

            server.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, OnReceive, server);
        }
    }
}
