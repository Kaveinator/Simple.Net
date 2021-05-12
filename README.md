# Warning!
This library is under testing, currently this is considered the experimental version, so expect bugs and _please_ report them!

# Simple.Net
A simple networking library that is for TCP and UDP. This is a hybrid of LiteNetLib networking solution (Link: https://github.com/RevenantX/LiteNetLib) and my SimpleJSON.Net library.

# Documentation
# Simple.Net.Server namespace
## Server class
### Static objects
`void GetRandomUnusedPort()`
 - Will return an available port to listen on

### Constructors
`Server(int port, Action<User> onConnect, Action<User> onDisconnect, int bufferSize = 2048)`
 - Will initilize the server instance, but will not listen. Use the Listen method
 - `bufferSize` the maximum ammount of data that can be transfered at one time (in bytes)

### Varaibles
`Socket socket` (read-only)
 - The server socket instance

`List<User> users` (read-only)
 - The list of users that are connected to the server instance

`int bufferSize` (read-only)
 - The set buffer size for the buffers

`int port` (read-only)
 - The servers listening port

### Methods
`void Close(User)`
 - Will close the connection to the user instance, but will not dispose it

`void broadcast<T>(T packet)`
 - Will send a packet to all connected clients

`void broadcast<T>(string groupName, T packet)`
 - Will send a packet to all connected clients that have the groupName in their `.groups` (Use User.groups to add/remove a group)
 
`void broadcastExcept<T>(T packet, int clientId)`
 - Will send a packet to all connected clients, except to a certain client with the clientId (Use User.clientId to get the uuid of the client)

`void broadcastExcept<T>(string groupName, T packet, int clientId)`
 - Will send a packet to all connected clients that have the groupName, except to a certain client with the clientId

## User class
### Static objects
-none-

### Constructors
`User(Socket socket, Server server)`
 - Only touch if you know what your doing, used by the Server class to create a client instance

### Varaibles
`int clientId` (read-only)
 - The uuid of the client instance

`Timer heartBeat`
 - The timer that sends a packet every 5 seconds to ensure that the peer is online (gonna be used to latency updates)

### Methods
`void emit<T>(T packet)`
 - Will send a packet

`bool eventExists<T>()`
 - Will check if a event handler for the event

`void removeEvent<T>()`
 - Will remove an event handler
 
`void on<T>(Action callback)`
 - Will call a function once a event has been received

`void on<T>(Action<User> callback)`
 - Will pass itself into a function once a event has been received

`void on<T>(Action<T> callback)`
 - Will call a function with the packet once a event has been received

`void on<T>(Action<User, T> callback)`
 - Will call a function with the user instance and the packet once a event has been received

`void Close()`
 - Will disconnect the client

# Simple.Net.Client namespace
## Client class
### Static objects
-none-

### Constructors
`public Client(string host, int port, Action onConnect, Action onDisconnect, int bufferSize = 2048)`
 - Will create an instance to connect to a server, but will not attempt to connect (use the `Connect()`)
 - `bufferSize` the maximum ammount of data that can be transfered at one time (in bytes)

### Variabled
`bool connected`
 - Is the instance connected to the server?

### Methods
`void Connect()`
 - Will attempt to connect to the server

`void emit<T>(T packet)`
 - Will send a packet

`bool eventExists<T>()`
 - Will check if a event handler for the event

`void removeEvent<T>()`
 - Will remove an event handler

`void on<T>(Action callback)`
 - Will call a function once a event has been received

`void on<T>(Action<User> callback)`
 - Will pass itself into a function once a event has been received

`void on<T>(Action<T> callback)`
 - Will call a function with the packet once a event has been received

`void on<T>(Action<User, T> callback)`
 - Will call a function with the user instance and the packet once a event has been received

`void Disconnect()`
 - Will disconnect the client

# Simple.Net namespace
## HashCache static class
### Variables
-none-
### Methods
`long Get<T>()`
 - Will return the hashcode of a class

`Type GetType()`
 - Will return a type from the hash code

## INetSerializable interface
Used for making events
### Implements Methods
`void Serialize(NetWriter)`
 - Used to serialize packets and send it over the network

`void Deserialize()`
 - Used to deserize from the network buffer

## NetBuffer class
Base buffer class
### Variables
`List<byte> buffer` (read-only)
 - The byte array without the hash code

`long hashCode` (read-only)
 - The hash code of the target event

### Methods
`byte[] ToByteArray()`
 - Converts a byte array with the data (including hash code)

`string ToString()`
 - Converts the buffer to a byte array, for debugging

## NetReader class (implements NetBuffer)
`public NetReader(byte[] buffer)`
 - Defines a net buffer from a buffer
 - Used by the `Simple.Net.Server.User` and `Simple.Net.Client.Client`

### Variables
-none-
### Methods
`bool ReadBool()`
 - Reads a boolean

`bool ReadInt64()`
 - Reads a long (whole number)

`bool ReadDouble()`
 - Reads a double (decimal number)

`bool ReadString()`
 - Reads a string

## NetWriter class (implements NetBuffer)
`public NetWriter(long hashCode)`
 - Defines a new NetWriter with the target event
 - Used by the `Simple.Net.Server.User` and `Simple.Net.Client.Client`

### Variables
-none-
### Methods
`void Push(bool value)`
 - Appends a boolean

`void Push(long value)`
 - Appends a long (whole number)

`void Push(double value)`
 - Appends a double (decimal number)

`void Push(string value)`
 - Appends a string
