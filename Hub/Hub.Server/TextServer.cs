using System.Net;
using System.Net.Sockets;
using Hub.Hooks;
namespace Hub.Server;

public class TextServer {
    const int BACKLOG = 32;
    const int BUFFER_SIZE = 1024;

    readonly Guid serverId = Guid.Empty;

    byte[] buffer = new byte[BUFFER_SIZE];
    Socket server = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    Dictionary<Socket, Guid> clientIds = [];
    Queue<Message> messages = [];

    public void Initialize(IPAddress address, int port) {
        Console.WriteLine("Starting Text Server...");

        buffer = new byte[BUFFER_SIZE];
        server = new(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        clientIds = [];
        messages = [];

        server.Bind(new IPEndPoint(address, port));
        server.Listen(BACKLOG);

        Console.WriteLine($"Text Server: Listening on port {port}");

        server.BeginAccept(OnAccept, null);

        Console.ReadLine();
    }

    /// <summary>
    /// This method is called when the server accepts a connection.
    /// </summary>
    void OnAccept(IAsyncResult result) {
        var client = server.EndAccept(result);
        var guid = Guid.NewGuid();
        clientIds.Add(client, guid);

        Console.WriteLine($"Client connected with TextID: {guid}");

        // Start receiving data from the client
        client.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, OnReceive, client);

        // Accept the next connection
        server.BeginAccept(OnAccept, null);
        
        Console.WriteLine(client.RemoteEndPoint);

        var registrationMessage = MessageUtils.CreateRegistration(serverId, guid);
        var bytes = registrationMessage.SerializeMessage();
        client.BeginSend(bytes, 0, bytes.Length, SocketFlags.None, OnSend, client);
    }

    /// <summary>
    /// This method is called when the server receives a message.
    /// </summary>
    void OnReceive(IAsyncResult result) {
        if (result.AsyncState is not Socket client) {
            Console.WriteLine("Could not send message.");
            return;
        }
        var bytesReceived = client.EndReceive(result);

        var messageBytes = new byte[bytesReceived];
        Buffer.BlockCopy(buffer, 0, messageBytes, 0, bytesReceived);
        var message = messageBytes.DeserializeMessage();

        if (message.Header != MessageType.Text || message.SenderId == Guid.Empty) return;

        var expectedId = clientIds[client];
        if (message.SenderId != expectedId) return;

        Console.WriteLine($"{message}: {message.DeserializeText()}");
        messages.Enqueue(message);

        client.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, OnReceive, client);

        while(messages.Count > 0) {
            var nextMessage = messages.Dequeue();
            var bytes = nextMessage.SerializeMessage();
            foreach (var c in clientIds.Keys) {
                Console.WriteLine($"Sending: <{message}: {nextMessage.DeserializeText()}> to {clientIds[client]}");
                c.BeginSend(bytes, 0, bytes.Length, SocketFlags.None, OnSend, c);
            }
        }
    }

    /// <summary>
    /// This method is called when the server sends a message.
    /// </summary>
    /// <param name="result"></param>
    void OnSend(IAsyncResult result) {
        if (result.AsyncState is not Socket client) {
            Console.WriteLine("Could not send message.");
            return;
        }
        client.EndSend(result);
    }
}
