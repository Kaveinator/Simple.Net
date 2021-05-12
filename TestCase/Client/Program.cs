using System;
using Simple.Net.Client;
using NetworkEvents;

namespace SimpleClient
{
    static class Program
    {
        static Client Network;

        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            Network = new Client("localhost", 5483, () => Console.WriteLine("Connected!"), () => Console.WriteLine("Disconnected!"));
            Network.on<Test>((Test packet) => Console.WriteLine(packet.testString));
            Network.Connect();
            while (true) {
                string input = Console.ReadLine();
                if (input == "exit") break;
                Network.emit<Test>(new Test() { testString = input });
            }
        }
    }
}
