using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Hub.Hooks;
using UnityEngine;
namespace _Project.Scripts {
    public class PositionClient : MonoBehaviour {
        const int BUFFER_SIZE = 1024;
        const int TIMEOUT = 500;

        Guid clientId = Guid.Empty;

        byte[] buffer;
        Socket client;
        IPEndPoint remoteEp;
        EndPoint remote;

        public void Initialize(IPAddress address, int port) {
            Debug.Log("Starting Position Client...");

            buffer = new byte[BUFFER_SIZE];
            remoteEp = new(address, port);
            client = new(address.AddressFamily, SocketType.Dgram, ProtocolType.Udp);

            client.Bind(new IPEndPoint(IPAddress.Any, 0));

            remote = remoteEp; // implicit cast to EndPoint
            client.BeginReceiveFrom(buffer, 0, BUFFER_SIZE, SocketFlags.None, ref remote, OnReceive, client);
            _ = StartSending();
        }
        void OnReceive(IAsyncResult result) {
            var bytesReceived = client.EndReceiveFrom(result, ref remote);
            Debug.Log($"Received {bytesReceived} bytes from {remote}");

            var bytes = new byte[bytesReceived];
            Buffer.BlockCopy(buffer, 0, bytes, 0, bytesReceived);

            var message = bytes.DeserializeMessage();

            switch(message.Header) {
                case MessageType.Registration: {
                    if (Client.ClientId != Guid.Empty) {
                        client.BeginReceiveFrom(buffer, 0, BUFFER_SIZE, SocketFlags.None, ref remote, OnReceive, client);
                        return;
                    }
                    var id = message.DeserializeGuid();
                    clientId = id;
                    Client.ClientId = clientId;
                    Debug.Log($"Registered with PositionID: {clientId}");
                }
                    break;
                case MessageType.Position: {
                    if (clientId == Guid.Empty || message.SenderId == Client.ClientId) {
                        client.BeginReceiveFrom(buffer, 0, BUFFER_SIZE, SocketFlags.None, ref remote, OnReceive, client);
                        return;
                    }
                    var position = message.DeserializePosition();
                    var pos = new Vector3(position.Item1, position.Item2, position.Item3);
                    Debug.Log($"Received position from {remote}: {message.SenderId},{pos}");
                    Client.Messages.Enqueue(message);
                }
                    break;
                default: {
                    Debug.Log($"Received unknown message from {remote}");
                }
                    break;
            }
            client.BeginReceiveFrom(buffer, 0, BUFFER_SIZE, SocketFlags.None, ref remote, OnReceive, client);
        }
        async Task StartSending() {
            for (;;) {
                try {
                    var pos = GameManager.Position;
                    var messageBytes = MessageUtils.CreatePosition(clientId, pos.x, pos.y, pos.z).SerializeMessage()!;
                    await client.SendToAsync(messageBytes, SocketFlags.None, remoteEp);
                    //Debug.Log($"Sent {pos} to {remoteEp}");
                } catch (Exception e) {
                    Debug.Log(e);
                }
                await Awaitable.WaitForSecondsAsync(TIMEOUT / 1000f);
            }
        }
    }
}
