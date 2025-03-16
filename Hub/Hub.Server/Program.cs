using System.Net;
namespace Server.Server;

internal abstract class Program {
    static TextServer textServer = new();
    static void Main(string[] args) {
        textServer = new();
        textServer.Initialize(IPAddress.Loopback, 6969);
        Console.ReadLine();
    }
}
