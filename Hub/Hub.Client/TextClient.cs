using System.Net;
using System.Net.Sockets;
using Hub.Hooks;
namespace Hub.Client;

internal class TextClient {
    const int BUFFER_SIZE = 1024;
    Guid clientId = Guid.Empty;

    byte[] buffer = new byte[BUFFER_SIZE];
    Socket client = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

    public void Initialize(IPAddress address, int port) {
        Console.WriteLine("Starting Client...");

        buffer = new byte[BUFFER_SIZE];
        client = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        client.Connect(address, port);
        Console.WriteLine("Connected to the server");

        // Start receiving data from the server
        client.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, OnReceive, client);
    }

    public void Send(string input) {
        if (clientId == Guid.Empty) {
            Console.WriteLine("The server has not assigned you an ID yet.");
            return;
        }

        var message = MessageUtils.CreateText(clientId, input);
        var bytes = message.SerializeMessage();
        client.BeginSend(bytes, 0, bytes.Length, SocketFlags.None, OnSend, client);
    }

    void OnSend(IAsyncResult result) {
        if (result.AsyncState is Socket server) {
            server.EndSend(result);
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
                Console.WriteLine($"{message}: {text}");
            }
                break;
            case MessageType.Registration: {
                clientId = message.DeserializeGuid();
                Console.WriteLine($"Registered with ID: {clientId}");
            }
                break;
            case MessageType.Position:
            case MessageType.Error:
            default: break;
        }

        server.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, OnReceive, server);
    }
}
