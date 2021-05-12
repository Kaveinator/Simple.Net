using System;
using Simple.Net.Server;
using NetworkEvents;

namespace SimpleServer
{
    static class Program
    {
        static Server Network;
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            Network = new Server(5483, OnConnect, user => Console.WriteLine("User Disconnected"));
            Network.Listen();    

            while (true) {
                string input = Console.ReadLine();
                if (input == "exit") break;
                Network.broadcast<Test>(new Test() { testString = input });
            }
        }

        static void OnConnect(User user) {
            Console.WriteLine("User Connected");
            user.on<Test>((Test packet) => Console.WriteLine(packet.testString));
        }
    }
}
