using System.Net;
namespace Hub.Server;

internal abstract class Program {
    static TextServer textServer = new();
    static PositionServer positionServer = new();
    static void Main(string[] args) {
        Thread textThread = new(InitializeTextServer);
        Thread positionThread = new(InitializePositionServer);
        
        textThread.Start();
        positionThread.Start();
        
        Console.ReadLine();
    }

    static void InitializeTextServer() {
        textServer = new();
        textServer.Initialize(IPAddress.Loopback, 6969);
    }
    
    static void InitializePositionServer() {
        positionServer = new();
        positionServer.Initialize(IPAddress.Loopback, 6970);
    }
}
