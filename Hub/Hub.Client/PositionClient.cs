using System.Net;
using System.Net.Sockets;
using Hub.Hooks;
namespace Hub.Client;

internal class PositionClient {
    const int BUFFER_SIZE = 1024;
    const int TIMEOUT = 500;
    Guid clientId = Guid.Empty;

    byte[] buffer = new byte[BUFFER_SIZE];
    Socket client = new(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
    IPEndPoint remoteEp = new(IPAddress.Any, 0);
    EndPoint remote = new IPEndPoint(IPAddress.Any, 0);

    public void Initialize(IPAddress address, int port) {
        Console.WriteLine("Starting Position Client...");

        remoteEp = new(address, port);
        client = new(address.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
        
        client.Bind(new IPEndPoint(IPAddress.Any, 0));

        remote = remoteEp; // implicit cast to EndPoint
        client.BeginReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, ref remote, OnReceive, client);
        Task.Run(StartSending);
    }
    void OnReceive(IAsyncResult result) {
        var bytesReceived = client.EndReceiveFrom(result, ref remote);
        Console.WriteLine($"Received {bytesReceived} bytes from {remote}");
        
        var bytes = new byte[bytesReceived];
        Buffer.BlockCopy(buffer, 0, bytes, 0, bytesReceived);
        
        var message = bytes.DeserializeMessage();

        switch(message.Header) {
            case MessageType.Registration: {
                var id = message.DeserializeGuid();
                clientId = id;
                Console.WriteLine($"Registered with PositionID: {clientId}");
            } break;
            case MessageType.Position: {
                var position = message.DeserializePosition();
                Console.WriteLine($"Received position from {remote}: {position}");
            } break;
            default: {
                Console.WriteLine($"Received unknown message from {remote}");
            } break;
        }
        client.BeginReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, ref remote, OnReceive, client);
    }
    async Task StartSending() {
        for (;;) {
            try {
                var randomX = (float)new Random().NextDouble() * 10;
                var messageBytes = MessageUtils.CreatePosition(clientId, randomX, 0, 0).SerializeMessage()!;
                await client.SendToAsync(messageBytes, remoteEp);
                Console.WriteLine($"Sent position to {remoteEp}");
            } catch (Exception e) {
                Console.WriteLine(e);
            }
            await Task.Delay(TIMEOUT);
        }
    }
}
