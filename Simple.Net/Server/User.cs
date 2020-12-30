using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace Simple.Net.Server {
    public class User {
        public int id { get; private set; }
        public IPEndPoint endPoint { get; private set; }
        static int ClientIdCounter = 0;
        public Server server { get; private set; }
        Dictionary<string, object> data = new Dictionary<string, object>();
        public object this[string index] {
            get => data[index];
            set => data[index] = index;
        }

        public TCP tcp { get; private set; }
        public UDP udp { get; private set; }
        public delegate void SocketEventHandler(Packet packet);
        public delegate void SocketEventHandlerWithoutParam();

        public User(TcpClient socket, Server server) {
            id = ClientIdCounter++;
            this.server = server;
            endPoint = (IPEndPoint)socket.GetStream().Socket.RemoteEndPoint;

            tcp = new TCP(socket, this);
        }

        public class TCP {
            public TcpClient socket { get; private set; }
            public NetworkStream networkStream { get; private set; }
            byte[] buffer;
            User user;
            Dictionary<string, object> SocketEvents = new Dictionary<string, object>();

            public TCP(TcpClient socket, User user) {
                if (!socket.Connected) throw new SocketException((int)SocketError.NotConnected);
                this.socket = socket;
                this.user = user;
                
                networkStream = socket.GetStream();
                buffer = new byte[user.server.bufferSize];
                
                Receive();
                user.udp = new UDP(user, this);
            }

            void Receive() {
                networkStream.BeginRead(buffer, 0, buffer.Length, (IAsyncResult asyncResult) => {
                    byte[] _buffer = new byte[networkStream.EndRead(asyncResult)];
                    if (_buffer.Length <= 0) { user.server.Close(user); return; }
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
                try {
                    Packet _packet = new Packet();
                    _packet.Write(eventName);
                    byte[] _buffer = _packet.ToByteArray();
                    networkStream.Write(_packet.ToByteArray(), 0, _packet.Length);
                } catch { user.server.Close(user); }
            }

            public void emit(string eventName, Packet packet) {
                try {
                    Packet _packet = Packet.Parse(packet.ToByteArray());
                    _packet.Insert(eventName);
                    networkStream.Write(_packet.ToByteArray(), 0, _packet.Length);
                } catch { user.server.Close(user); }
            }

            public async Task emitAsync(string eventName) {
                try {
                    Packet _packet = new Packet();
                    _packet.Write(eventName);
                    byte[] _buffer = _packet.ToByteArray();
                    await networkStream.WriteAsync(_packet.ToByteArray(), 0, _packet.Length, CancellationToken.None);
                } catch { user.server.Close(user); }
            }

            public async Task emitAsync(string eventName, Packet packet) {
                try {
                    Packet _packet = Packet.Parse(packet.ToByteArray());
                    _packet.Insert(eventName);
                    await networkStream.WriteAsync(_packet.ToByteArray(), 0, _packet.Length, CancellationToken.None);
                } catch { user.server.Close(user); }
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
            public Socket socket { get => user.server.udp; }
            byte[] buffer;
            EndPoint endPoint;
            User user;
            Dictionary<string, object> SocketEvents = new Dictionary<string, object>();

            public UDP(User user, TCP tcp) {
                this.user = user;
                buffer = new byte[user.server.bufferSize];
                System.Console.WriteLine("1 - Initilized");
                endPoint = tcp.socket.Client.RemoteEndPoint;
                Receive(endPoint);
            }

            void Receive(EndPoint ep) {
                user.server.udp.BeginReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, ref ep, (IAsyncResult asyncResult) => {
                    byte[] _buffer = new byte[user.server.udp.EndReceiveFrom(asyncResult, ref ep)];
                    System.Console.WriteLine("2 - Received");
                    Array.Copy(buffer, _buffer, _buffer.Length);
                    Receive(ep);

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
                try {
                    Packet _packet = new Packet();
                    _packet.Write(eventName);
                    user.server.udp.SendTo(_packet.ToByteArray(), 0, _packet.Length, SocketFlags.None, endPoint);
                } catch {}
            }

            public void emit(string eventName, Packet packet) {
                try {
                    Packet _packet = Packet.Parse(packet.ToByteArray());
                    _packet.Insert(eventName);
                    user.server.udp.SendTo(_packet.ToByteArray(), 0, _packet.Length, SocketFlags.None, endPoint);
                } catch {}
            }

            public async Task emitAsync(string eventName) {
                try {
                    Packet _packet = new Packet();
                    _packet.Write(eventName);
                    await user.server.udp.SendToAsync(_packet.ToByteArray(), SocketFlags.None, endPoint);
                } catch {}
            }

            public async Task emitAsync(string eventName, Packet packet) {
                try {
                    Packet _packet = Packet.Parse(packet.ToByteArray());
                    _packet.Insert(eventName);
                    await user.server.udp.SendToAsync(_packet.ToByteArray(), SocketFlags.None, endPoint);
                } catch {}
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

            public void Dispose() {
                System.Console.WriteLine("-1 - Disposed");
                //buffer = null;
                SocketEvents.Clear();
                SocketEvents = null;
            }
        }
    }
}