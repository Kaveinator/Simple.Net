using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace Simple.Net.Server
{
    public class Server {
        public IPEndPoint endPoint { get; private set; }
        public int bufferSize { get; private set; }
        public List<User> users { get; private set; } = new List<User>();
        public TcpListener tcp { get; private set; }
        public Socket udp { get; private set; }
        public delegate void ConEvent(User user);
        ConEvent OnConnectEvent;
        ConEvent OnDisconnectEvent;
        
        public Server(int port, int bufferSize, ConEvent onConnect, ConEvent onDisconnect) {
            this.bufferSize = bufferSize;
            OnConnectEvent = onConnect;
            OnDisconnectEvent = onDisconnect;

            endPoint = new IPEndPoint(IPAddress.Parse("0.0.0.0"), port);
            tcp = new TcpListener(endPoint);
            tcp.Start();
            udp = new Socket(SocketType.Dgram, ProtocolType.Udp);
            udp.Bind(endPoint);

            ListenTCP();
        }

        void ListenTCP() {
            tcp.BeginAcceptTcpClient((IAsyncResult asyncResult) => {
                TcpClient _socket;
                try { _socket = tcp.EndAcceptTcpClient(asyncResult); }
                catch { ListenTCP(); return; }
                ListenTCP();

                User _user = new User(_socket, this);
                users.Add(_user);
                OnConnectEvent(_user);
            }, null);
        }

        public void Stop() {
            CloseAll();
            tcp.Stop();
            udp.Close();
            udp.Dispose();
        }

        public void CloseAll() {
            foreach (User user in users)
                Close(user);
        }

        public void Close(User user) {
            // TODO: Finish close
            user.tcp.Dispose();
            user.udp.Dispose();
            OnDisconnectEvent(user);
            users.Remove(user);
        }

        public void SendToAll(string eventName) {
            foreach (User user in users)
                user.tcp.emit(eventName);
        }

        public void SendToAll(string eventName, Packet packet) {
            foreach (User user in users)
                user.tcp.emit(eventName, packet);
        }

        public async Task SendToAllAsync(string eventName) {
            if (users.Count == 0) return;
            Task[] _tasks = new Task[users.Count];
            for (int i = 0; i < _tasks.Length; i++)
                _tasks[i] = users[i].tcp.emitAsync(eventName);
            await Task.WhenAll(_tasks);
        }

        public async Task SendToAllAsync(string eventName, Packet packet) {
            if (users.Count == 0) return;
            Task[] _tasks = new Task[users.Count];
            for (int i = 0; i < _tasks.Length; i++)
                _tasks[i] = users[i].tcp.emitAsync(eventName, packet);
            await Task.WhenAll(_tasks);
        }

        public void SendToAllUDP(string eventName) {
            foreach (User user in users)
                user.udp.emit(eventName);
        }

        public void SendToAllUDP(string eventName, Packet packet) {
            foreach (User user in users)
                user.udp.emit(eventName, packet);
        }
    
        public async Task SendToAllUDPAsync(string eventName) {
            if (users.Count == 0) return;
            Task[] _tasks = new Task[users.Count];
            for (int i = 0; i < _tasks.Length; i++)
                _tasks[i] = users[i].udp.emitAsync(eventName);
            await Task.WhenAll(_tasks);
        }

        public async Task SendToAllUDPAsync(string eventName, Packet packet) {
            if (users.Count == 0) return;
            Task[] _tasks = new Task[users.Count];
            for (int i = 0; i < _tasks.Length; i++)
                _tasks[i] = users[i].udp.emitAsync(eventName, packet);
            await Task.WhenAll(_tasks);
        }
    }
}