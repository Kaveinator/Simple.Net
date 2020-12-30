using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace Simple.Net.Client
{
    public class Client {
        public IPEndPoint remoteEndPoint { get; private set; }
        public int bufferSize { get; private set; }
        public delegate void ConEvent();
        ConEvent OnConnectEvent;
        ConEvent OnDisconnectEvent;
        public TCP tcp { get; private set; }
        public UDP udp { get; private set; }
        public delegate void SocketEventHandler(Packet packet);
        public delegate void SocketEventHandlerWithoutParam();

        public Client(IPAddress server, int port, int bufferSize, ConEvent onConnect, ConEvent onDisconnect) {
            remoteEndPoint = new IPEndPoint(server, port);
            this.bufferSize = bufferSize;
            OnConnectEvent = onConnect;
            OnDisconnectEvent = onDisconnect;

            tcp = new TCP(this);
        }

        public void Close() {
            tcp.Dispose();
            udp.Dispose();
            OnDisconnectEvent();
        }

        public class TCP {
            public TcpClient socket { get; private set; }
            public NetworkStream networkStream { get; private set; }
            byte[] buffer;
            Client client;
            Dictionary<string, object> SocketEvents = new Dictionary<string, object>();

            public TCP(Client client) {
                this.client = client;
                buffer = new byte[client.bufferSize];

                socket = new TcpClient(AddressFamily.InterNetwork);
                Connect();
            }

            void Connect() {
                socket.BeginConnect(client.remoteEndPoint.Address, client.remoteEndPoint.Port, (IAsyncResult asyncResult) => {
                    socket.EndConnect(asyncResult);
                    networkStream = socket.GetStream();
                    client.udp = new UDP(client);
                    client.OnConnectEvent();
                    Receive();
                }, null);
            }

            void Receive() {
                networkStream.BeginRead(buffer, 0, buffer.Length, (IAsyncResult asyncResult) => {
                    byte[] _buffer = new byte[networkStream.EndRead(asyncResult)];
                    if (_buffer.Length <= 0) { client.Close(); return; }
                    Array.Copy(buffer, _buffer, _buffer.Length);
                    Receive();

                    Packet _packet = Packet.Parse(_buffer);
                    string _eventName = _packet.readHeader();
                    if (SocketEvents.ContainsKey(_eventName)) {
                        if (SocketEvents[_eventName].GetType() == typeof(SocketEventHandler))
                            ((SocketEventHandler)SocketEvents[_eventName])(_packet);
                        else ((SocketEventHandlerWithoutParam)SocketEvents[_eventName])();
                    }
                    else {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"TCP SOCKET ERROR - No callback registered for '{_eventName}'");
                        Console.ResetColor();
                    }
                }, null);
            }

            public void on(string eventName, SocketEventHandler callback) {
                if (SocketEvents.ContainsKey(eventName))
                    SocketEvents[eventName] = callback;
                else SocketEvents.Add(eventName, callback);
            }

            public void on(string eventName, SocketEventHandlerWithoutParam callback) {
                if (SocketEvents.ContainsKey(eventName))
                    SocketEvents[eventName] = callback;
                else SocketEvents.Add(eventName, callback);
            }

            public bool ContainsEvent(string eventName) => SocketEvents.ContainsKey(eventName);

            public void RemoveEvent(string eventName) {
                if (ContainsEvent(eventName))
                    SocketEvents.Remove(eventName);
            }

            public void emit(string eventName) {
                if (string.IsNullOrEmpty(eventName)) return;
                Packet _packet = new Packet();
                _packet.Write(eventName);
                networkStream.Write(_packet.ToByteArray(), 0, _packet.Length);
            }

            public void emit(string eventName, Packet packet) {
                if (string.IsNullOrEmpty(eventName) || packet == null) return;
                Packet _packet = new Packet();
                _packet.Insert(eventName);
                networkStream.Write(_packet.ToByteArray(), 0, _packet.Length);
            }

            public async Task emitAsync(string eventName) {
                if (string.IsNullOrEmpty(eventName)) return;
                Packet _packet = new Packet();
                _packet.Write(eventName);
                byte[] _buffer = _packet.ToByteArray();
                await networkStream.WriteAsync(_buffer, 0, _buffer.Length, CancellationToken.None);
            }

            public async Task emitAsync(string eventName, Packet packet) {
                if (string.IsNullOrEmpty(eventName) || packet == null) return;
                packet.Insert(eventName);
                byte[] _buffer = packet.ToByteArray();
                await networkStream.WriteAsync(_buffer, 0, _buffer.Length, CancellationToken.None);
            }
            
            public void Dispose() {
                socket.Close();
                networkStream = null;
                buffer = null;
                socket = null;
                SocketEvents.Clear();
                SocketEvents = null;
            }   
        }
        public class UDP {
            public UdpClient socket { get; private set; }
            Client client;
            IPEndPoint endPoint;
            Dictionary<string, object> SocketEvents = new Dictionary<string, object>();

            public UDP(Client client) {
                this.client = client;

                socket = new UdpClient(((IPEndPoint)client.tcp.socket.Client.LocalEndPoint).Port);
                endPoint = (IPEndPoint)client.tcp.socket.Client.RemoteEndPoint;
                socket.Connect(endPoint);

                Receive(endPoint);
            }

            void Receive(IPEndPoint endPoint) {
                socket.BeginReceive((IAsyncResult asyncResult) => {
                    byte[] _buffer = socket.EndReceive(asyncResult, ref endPoint);
                    Receive(endPoint);

                    Packet _packet = Packet.Parse(_buffer);
                    string _eventName = _packet.readHeader();
                    if (SocketEvents.ContainsKey(_eventName)) {
                        if (SocketEvents[_eventName].GetType() == typeof(SocketEventHandler))
                            ((SocketEventHandler)SocketEvents[_eventName])(_packet);
                        else ((SocketEventHandlerWithoutParam)SocketEvents[_eventName])();
                    }
                    else {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"UDP SOCKET ERROR - No callback registered for '{_eventName}'");
                        Console.ResetColor();
                    }
                }, null);
            }

            public void emit(string eventName) {
                if (string.IsNullOrEmpty(eventName)) return;
                Packet _packet = new Packet();
                _packet.Write(eventName);
                socket.BeginSend(_packet.ToByteArray(), _packet.Length, (IAsyncResult asyncResult) => socket.EndSend(asyncResult), null);
            }

            public void emit(string eventName, Packet packet) {
                if (string.IsNullOrEmpty(eventName) || packet == null) return;
                Packet _packet = Packet.Parse(packet.ToByteArray());
                _packet.Insert(eventName);
                socket.BeginSend(_packet.ToByteArray(), _packet.Length, (IAsyncResult asyncResult) => socket.EndSend(asyncResult), null);
            }

            public async Task emitAsync(string eventName) {
                if (string.IsNullOrEmpty(eventName)) return;
                Packet _packet = new Packet();
                _packet.Write(eventName);
                await socket.SendAsync(_packet.ToByteArray(), _packet.Length, null);
            }

            public async Task emitAsync(string eventName, Packet packet) {
                if (string.IsNullOrEmpty(eventName) || packet == null) return;
                Packet _packet = Packet.Parse(packet.ToByteArray());
                _packet.Insert(eventName);
                await socket.SendAsync(_packet.ToByteArray(), _packet.Length, null);
            }

            public void on(string eventName, SocketEventHandler callback) {
                if (string.IsNullOrEmpty(eventName) || callback == null) return;
                if (SocketEvents.ContainsKey(eventName))
                    SocketEvents[eventName] = callback;
                else SocketEvents.Add(eventName, callback);
            }

            public void on(string eventName, SocketEventHandlerWithoutParam callback) {
                if (string.IsNullOrEmpty(eventName) || callback == null) return;
                if (SocketEvents.ContainsKey(eventName))
                    SocketEvents[eventName] = callback;
                else SocketEvents.Add(eventName, callback);
            }

            public void Dispose() {
                socket.Close();
                socket = null;
                SocketEvents.Clear();
                SocketEvents = null;
            }
        }
    }
}