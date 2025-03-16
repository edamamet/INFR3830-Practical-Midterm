using System.Net;
using System.Net.Sockets;
using Hub.Hooks;
namespace Hub.Server;

public class PositionServer {
    const int BUFFER_SIZE = 1024;
    const int TIMEOUT = 500;

    readonly Guid serverId = Guid.Empty;

    byte[] buffer = new byte[BUFFER_SIZE];
    Socket server = new(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
    IPEndPoint remote = new(IPAddress.Any, 0);
    Dictionary<IPEndPoint, Guid> clientIds = [];
    Queue<Message> messages = [];

    public void Initialize(IPAddress address, int port) {
        Console.WriteLine("Starting Position Server...");
        buffer = new byte[BUFFER_SIZE];
        server = new(address.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
        remote = new(IPAddress.Any, port);

        server.Bind(new IPEndPoint(address, port));

        Console.WriteLine($"Position Server: Listening on port {port}");

        _ = Task.Run(ReceiveLoop);
        _ = Task.Run(SendLoop);
    }

    async Task ReceiveLoop() {
        for (;;) {
            var result = await server.ReceiveFromAsync(buffer, SocketFlags.None, remote);
            var bytesReceived = result.ReceivedBytes;
            var endPoint = (IPEndPoint)result.RemoteEndPoint;
            
            Console.WriteLine(endPoint);
            
            /*
            var bytes = new byte[bytesReceived];
            Buffer.BlockCopy(buffer, 0, bytes, 0, bytesReceived);
            
            var message = bytes.DeserializeMessage();
            if (message.Header == MessageType.Registration) {
                var guid = message.DeserializeGuid();
                clientIds.Add(endPoint, guid);
                Console.WriteLine($"Client connected with ID: {guid}");
            } else {
                messages.Enqueue(message);
            }
        */
        }
    }
    
    async Task SendLoop() {
        for (;;) {
            if (messages.Count == 0) {
                await Task.Delay(TIMEOUT);
                continue;
            }

            var message = messages.Dequeue();
            var bytes = message.SerializeMessage();
            await server.SendToAsync(bytes, SocketFlags.None, remote);
        }
    }
}
