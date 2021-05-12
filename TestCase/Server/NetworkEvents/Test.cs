using System;
using Simple.Net;

namespace NetworkEvents {
    public struct Test : INetSerializable {
        public string testString;

        public void Serialize(NetWriter writer) {
            Console.WriteLine("Serialize called!");
            writer.Push(testString);
        }

        public void Deserialize(NetReader reader) {
            Console.WriteLine("Deserialize called!");
            testString = reader.ReadString();
        }
    }
}