using System.Net;
using System.Net.Sockets;
using DehxServerLib.Network;
using DehxServerLib.ServerMessaging;

namespace DehxServerLib
{

    public class DehxServer
    {

        public List<TcpListener> TCPlisteners;
        public List<Connection> connections;
        public List<IPAddress> serverIPs;
        public ServerMessageHandlerCallback serverMessageHandler;

        public void DropConnection(int clientId)
        {
            Console.WriteLine("Client {0} Disconnected!", clientId.ToString());
            try
            {

                connections.Remove(connections.Single(cid => cid.ClientId == clientId));
            }
            catch (Exception)
            {

            }
        }

        public IPAddress[] GetIPAddresses()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            return host.AddressList;
        }

        public DehxServer(ServerMessageHandlerCallback smh)
        {

            this.serverMessageHandler = smh;
            Int32 tcpport = 1337;
            connections = new List<Connection>();
            Console.Write("Discovering IP Addresses ... ");
            serverIPs = GetIPAddresses().ToList();
            serverIPs.Add(new IPAddress(new byte[] { 127, 0, 0, 1 }));
            Console.Write("{0} found ... ", serverIPs.Count);
            Console.WriteLine();
            TCPlisteners = new List<TcpListener>();

            foreach (var ip in serverIPs)
            {
                Console.Write($"Attempting to listen on {ip}:{tcpport} ... ");
                try
                {
                    TCPlisteners.Add(new TcpListener(ip, tcpport));
                    TCPlisteners.Last().Start();
                    Console.Write(" SUCCESS");
                    Console.WriteLine();
                }
                catch (SocketException e)
                {
                    Console.Write(" ERROR.");
                    Console.WriteLine();
                }



            }

            Console.WriteLine("********** SERVER READY FOR CONNECTIONS *********");
            Task.Run(() =>
            {
                Parallel.ForEach(TCPlisteners, listener =>
                {
                    AcceptIncomingConnections(listener);
                });
            });

        }

        public Task AcceptIncomingConnections(TcpListener listener)
        {
            while (true)
            {
                TcpClient client = listener.AcceptTcpClient();
                DisconnectedCallback dc = DropConnection;
                int clientid = IDNumbers.NextId();
                connections.Add(new Connection(client, clientid, dc, serverMessageHandler, this));
                Console.WriteLine("Client {0} {1} Connected!", clientid.ToString(), client.Client.RemoteEndPoint?.ToString());
                Task.Yield();
            }
        }

    }
}