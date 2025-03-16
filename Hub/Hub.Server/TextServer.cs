using System.Net;
using System.Net.Sockets;
using Hub.Hooks;
namespace Server.Server;

public class TextServer {
    const int BACKLOG = 32;
    const int BUFFER_SIZE = 1024;
    const int TIMEOUT = 500;

    readonly Guid serverId = Guid.Empty;

    byte[] buffer = new byte[BUFFER_SIZE];
    Socket server = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    Dictionary<Socket, Guid> clientIds = [];
    Queue<Message> messages = [];

    public void Initialize(IPAddress address, int port) {
        Console.WriteLine("Starting Server...");

        buffer = new byte[BUFFER_SIZE];
        server = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        clientIds = [];
        messages = [];

        server.Bind(new IPEndPoint(address, port));
        server.Listen(BACKLOG);

        Console.WriteLine($"Listening on port {port}");

        server.BeginAccept(OnAccept, null);

        SendLoop();

        Console.ReadLine();
    }

    /// <summary>
    /// This method is called when the server accepts a connection.
    /// </summary>
    void OnAccept(IAsyncResult result) {
        var client = server.EndAccept(result);
        var guid = Guid.NewGuid();
        clientIds.Add(client, guid);

        Console.WriteLine($"Client connected with ID: {guid}");

        // Start receiving data from the client
        client.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, OnReceive, client);

        // Accept the next connection
        server.BeginAccept(OnAccept, null);

        var registrationMessage = MessageUtils.CreateRegistration(serverId, guid);
        var bytes = registrationMessage.SerializeMessage();
        client.BeginSend(bytes, 0, bytes.Length, SocketFlags.None, OnSend, client);
    }

    /// <summary>
    /// This method is called when the server receives a message.
    /// </summary>
    void OnReceive(IAsyncResult result) {
        var client = (Socket)result.AsyncState!;
        var bytesReceived = client.EndReceive(result);

        var messageBytes = new byte[bytesReceived];
        Buffer.BlockCopy(buffer, 0, messageBytes, 0, bytesReceived);
        var message = messageBytes.DeserializeMessage();

        if (message.Header != MessageType.Text || message.SenderId == Guid.Empty) return;

        var expectedId = clientIds[client];
        if (message.SenderId != expectedId) return;

        Console.WriteLine($"{message}: {message.Content.DeserializeText()}");
        messages.Enqueue(message);

        client.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, OnReceive, client);
    }

    /// <summary>
    /// This method is called when the server sends a message.
    /// </summary>
    /// <param name="result"></param>
    void OnSend(IAsyncResult result) {
        if (result.AsyncState is Socket client) {
            client.EndSend(result);
        }
    }

    void SendLoop() {
        for (;;) {
            while(messages.Count > 0) {
                var message = messages.Dequeue();
                var bytes = message.SerializeMessage();
                foreach (var client in clientIds.Keys) {
                    Console.WriteLine($"Sending: <{message}: {message.Content.DeserializeText()}> to {clientIds[client]}");
                    client.BeginSend(bytes, 0, bytes.Length, SocketFlags.None, OnSend, client);
                }
            }
            Thread.Sleep(TIMEOUT);
        }
    }
}
