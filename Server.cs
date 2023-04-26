using System.Net;
using System.Net.Sockets;

namespace DehxServerLib
{

    public delegate bool ServerMessageHandlerCallback(byte[] message, object remoteEP);

    public class DehxServer
    {
        public TcpListener tcpListener;
        public List<TcpClient> tcpClients = new List<TcpClient>();
        public UdpClient udpServer;
        public List<IPEndPoint> udpClients = new List<IPEndPoint>();
        public ServerMessageHandlerCallback serverMessageHandler;
        public bool IsRunning { get { return netThreadCancelRequest.IsCancellationRequested; } }
        private CancellationTokenSource netThreadCancelRequest;
        private int udpPort;
        public const int SIO_UDP_CONNRESET = -1744830452;


        public DehxServer(ServerMessageHandlerCallback smh, int tcpPort, int udpPort)
        {
            netThreadCancelRequest = new CancellationTokenSource();
            serverMessageHandler = smh;
            Thread ServerStartThread = new Thread(() =>
            {
                Start(tcpPort, udpPort, netThreadCancelRequest.Token);
            });
            ServerStartThread.Start();
        }

        public void Shutdown()
        {
            netThreadCancelRequest.Cancel();
        }

        public void Start(int tcpPort, int udpPort, CancellationToken cancellationToken)
        {
            this.udpPort = udpPort;
            // Start the TCP server
            tcpListener = new TcpListener(IPAddress.Any, tcpPort);
            tcpListener.Start();
            Console.WriteLine("TCP server started on port {0}", tcpPort);

            // Start the UDP server
            udpServer = new UdpClient(udpPort);
            udpServer.Client.SendBufferSize = 65507;
            udpServer.Client.ReceiveBufferSize = 65507;
            udpServer.Client.IOControl((IOControlCode)SIO_UDP_CONNRESET, new byte[] { 0, 0, 0, 0 }, null);
            Console.WriteLine("UDP server started on port {0}", udpPort);

            // Start accepting TCP clients in a new thread
            Thread tcpThread = new Thread(() =>
            {
                TcpThreadWorker(cancellationToken);
            });
            tcpThread.Start();

            // Start receiving UDP packets in a new thread
            Thread udpThread = new Thread(() =>
            {

                UdpThreadWorker(cancellationToken);

            });
            udpThread.Start();
        }

        private void TcpThreadWorker(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    TcpClient client = tcpListener.AcceptTcpClient();
                    client.Client.SendBufferSize = int.MaxValue;
                    client.Client.ReceiveBufferSize = int.MaxValue;
                    tcpClients.Add(client);
                    Console.WriteLine("TCP client connected from {0}", client.Client.RemoteEndPoint);
                    // Start a new thread to handle this client
                    Thread clientThread = new Thread(() => HandleTcpClient(client));
                    clientThread.Start();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }

        private void UdpThreadWorker(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                byte[] data = { };
                IPEndPoint remoteEP;

                remoteEP = new IPEndPoint(IPAddress.Any, 0);
                data = udpServer.Receive(ref remoteEP);
                if (!udpClients.Contains(remoteEP))
                {
                    udpClients.Add(remoteEP);
                    Console.WriteLine("UDP client connected from {0}", remoteEP);
                }

                HandleUdpPacket(data, remoteEP);
            }
        }


        private void HandleTcpClient(TcpClient tcpClient)
        {
            try
            {

                NetworkStream stream = tcpClient.GetStream();
                BinaryReader br = new BinaryReader(tcpClient.GetStream());
                while (tcpClient.Connected)
                {
                    if (stream.DataAvailable)
                    {
                        byte[] buffer = new byte[stream.Socket.Available];
                        int numberOfBytesRead = stream.Read(buffer, 0, buffer.Length);

                        if (serverMessageHandler != null)
                        {
                            if (serverMessageHandler(buffer, tcpClient.Client.RemoteEndPoint))
                            {
                                foreach (IPEndPoint otherClient in udpClients)
                                {
                                    udpServer.Send(buffer, buffer.Length, otherClient);
                                }
                            }
                        }
                    }
                }

                Console.WriteLine("TCP client disconnected from {0}", tcpClient.Client.RemoteEndPoint);
            }
            catch (SocketException ex)
            {
                Console.WriteLine(ex);
                return;
            }
        }

        private void HandleUdpPacket(byte[] data, IPEndPoint remoteEP)
        {
            // Handle the UDP packet
            if (serverMessageHandler != null)
            {
                if (serverMessageHandler(data, remoteEP))
                {
                    foreach (IPEndPoint otherClient in udpClients)
                    {
                        if (!otherClient.Equals(remoteEP))
                        {
                            udpServer.Send(data, data.Length, otherClient);
                        }
                    }
                }
            }
        }

        public void SendUdpBroadcast(byte[] data)
        {

            Parallel.ForEach(udpClients, udpClient =>
            {
                try
                {
                    udpServer.Send(data, data.Length, udpClient);
                }

                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            });

        }
        public void SendTcpBroadcast(byte[] data)
        {
            Parallel.ForEach(tcpClients, tcpClient =>
            {
                tcpClient.GetStream().Write(data, 0, data.Length);
            });

        }

        public void SendUdp(byte[] data, IPEndPoint remoteEP)
        {
            udpServer.Send(data, data.Length, remoteEP);
        }


        public void SendTcp(byte[] data, IPEndPoint remoteEP)
        {
            tcpClients.Single(t => t.Client.RemoteEndPoint == remoteEP).GetStream().Write(data, 0, data.Length);
        }
    }

}

