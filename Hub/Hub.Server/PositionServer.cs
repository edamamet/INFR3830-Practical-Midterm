using System.Net;
using System.Net.Sockets;
using Hub.Hooks;
namespace Hub.Server;

public class PositionServer {
    const int BUFFER_SIZE = 1024;
    const int TIMEOUT = 500;
    const float TOLERANCE = 0.01f;
    readonly Guid serverId = Guid.Empty;

    byte[] buffer = new byte[BUFFER_SIZE];
    Socket server;
    IPEndPoint localEp;
    EndPoint client;

    Dictionary<EndPoint, ClientInformation> clientIds = new();

    Queue<Message> messages = new();
    class ClientInformation(Guid clientId, float x, float y, float z) {
        public Guid ClientId = clientId;
        public float X = x, Y = y, Z = z;
    }
    public void Initialize(IPAddress address, int port) {
        localEp = new(address, port);
        server = new(address.AddressFamily, SocketType.Dgram, ProtocolType.Udp);

        var clientEp = new IPEndPoint(IPAddress.Any, 0);
        client = clientEp;

        server.Bind(localEp);

        Console.WriteLine($"Postition Server: Listening on {localEp}");
        server.BeginReceiveFrom(buffer, 0, BUFFER_SIZE, SocketFlags.None, ref client, OnReceive, server);
    }
    void OnReceive(IAsyncResult result) {
        var bytesReceived = server.EndReceiveFrom(result, ref client);

        var bytes = new byte[bytesReceived];
        Buffer.BlockCopy(buffer, 0, bytes, 0, bytesReceived);

        var message = bytes.DeserializeMessage();

        if (!clientIds.TryGetValue(client, out var clientInfo)) {
            var id = Guid.NewGuid();
            clientIds.Add(client, new(id, 0, 0, 0));
            Console.WriteLine($"Client not found, assigning new ID: {id}");
            var registrationMessage = MessageUtils.CreateRegistration(serverId, id);
            var registrationBytes = registrationMessage.SerializeMessage();
            server.BeginSendTo(registrationBytes, 0, registrationBytes.Length, SocketFlags.None, client, OnSend, server);
        }

        if (message.Header != MessageType.Position) {
            server.BeginReceiveFrom(buffer, 0, BUFFER_SIZE, SocketFlags.None, ref client, OnReceive, server);
            return;
        }

        clientInfo = clientIds[client];

        var position = message.DeserializePosition();
        if (   MathF.Abs(position.Item1 - clientInfo.X) < TOLERANCE
            && MathF.Abs(position.Item2 - clientInfo.Y) < TOLERANCE
            && MathF.Abs(position.Item3 - clientInfo.Z) < TOLERANCE) {
            server.BeginReceiveFrom(buffer, 0, BUFFER_SIZE, SocketFlags.None, ref client, OnReceive, server);
            return;
        }

        clientInfo.X = position.Item1;
        clientInfo.Y = position.Item2;
        clientInfo.Z = position.Item3;

        Console.WriteLine($"Received new position from {clientInfo.ClientId}: {position.Item1}, {position.Item2}, {position.Item3}");
        var positionMessage = MessageUtils.CreatePosition(clientInfo.ClientId, clientInfo.X, clientInfo.Y, clientInfo.Z);
        messages.Enqueue(positionMessage);

        server.BeginReceiveFrom(buffer, 0, BUFFER_SIZE, SocketFlags.None, ref client, OnReceive, server);
        Task.Run(StartSending);
    }
    void OnSend(IAsyncResult ar) { }

    void SendMessages() {
        while(messages.Count > 0) {
            var message = messages.Dequeue();
            var bytes = message.SerializeMessage();
            foreach (var tempClient in clientIds.Keys) {
                server.BeginSendTo(bytes, 0, bytes.Length, SocketFlags.None, tempClient, OnSend, server);
            }
            Console.WriteLine("Sent Messages");
        }
    }

    async Task StartSending() {
        for (;;) {
            try {
                SendMessages();
            } catch (Exception e) {
                Console.WriteLine(e);
            }
            await Task.Delay(TIMEOUT);
        }
    }
}
