# DehxServerLib

This is a simple server class to enable TCP/IP and UDP client connections for your games.

It uses callbacks that are executed once a tcp or udp packet is captured, so that you can do what you need.

There are also methods available to easily send byte[] packets from your client:


Quick Start:


Create a static instance of the server, so that it is acesible across threads. provide a ServerMessageHandler callback 
( Signature:  public static bool ServerMessageHandler(byte[] messageData, object endPoint) ), and provide your tcp and udp ports to listen on.


        public static DehxServer Server = new DehxServer(ServerMessageHandler, 41337, 41338);
        
To shutdown the server call:
        Server.Shutdown();
it will auto shut down all listening threads gracefully.

To access a list of all TCP Clients use 
        
        List<TcpClient> Server.tcpClients 

To see a list of active udp clients use

        List<IPEndPoint> Server.udpClients
        
Games often need a way to easily broadcast to all connections the current state of the game.  This can easily be done using:

    SendUdpBroadcast ( byte[] data )
    or
    SendTcpBroadcast ( byte[] data )

Or to send a mesage to a specific client endpoint:

    SendTcp ( byte[] data, IPEndPoint remoteEP ) <-- this will find the tcp client that uses that endpoint, and send the data on the stream.
    or    
    SendUdp ( byte[] data, IPEndPoint remoteEP )


and its that easy!   Enjoy coding!

-Dehx