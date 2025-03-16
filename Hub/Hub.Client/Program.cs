using System.Net;
namespace Hub.Client;

internal abstract class Program {
    static TextClient textClient = new();
    static void Main(string[] args) {
        textClient = new();
        textClient.Initialize(IPAddress.Loopback, 6969);

        for (;;) {
            Console.Write("> ");
            var input = Console.ReadLine();
            
            if (string.IsNullOrWhiteSpace(input)) continue;
            if (input == "exit") break;
            
            textClient.Send(input);
        }

        Console.ReadLine();
    }
}