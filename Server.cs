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

        public async void Start(int tcpPort, int udpPort, CancellationToken cancellationToken)
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


            Task receiveMessagesTask = Task.Run(async () => await ReceiveMessagesAsync(udpServer, cancellationToken), cancellationToken);
            Task acceptClientsTask = Task.Run(async () => await AcceptClientsAsync(tcpListener, cancellationToken), cancellationToken);

            await acceptClientsTask;
            await receiveMessagesTask;
        }

        async Task ReceiveMessagesAsync(UdpClient udpServer, CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    UdpReceiveResult result = await udpServer.ReceiveAsync().WithCancellation(cancellationToken);
                    byte[] data = result.Buffer;
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
            catch (OperationCanceledException)
            {
                Console.WriteLine("Receiving messages cancelled.");
            }
        }

        async Task AcceptClientsAsync(TcpListener listener, CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    TcpClient client = await listener.AcceptTcpClientAsync().WithCancellation(cancellationToken);

                    client.Client.SendBufferSize = int.MaxValue;
                    client.Client.ReceiveBufferSize = int.MaxValue;
                    tcpClients.Add(client);
                    Console.WriteLine("TCP client connected from {0}", client.Client.RemoteEndPoint);
                    // Start a new thread to handle this client
                    Thread clientThread = new Thread(() => HandleTcpClient(client));
                    clientThread.Start();
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Accepting clients cancelled.");
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

    public static class TaskExtensions
    {
        public static async Task<TResult> WithCancellation<TResult>(this Task<TResult> task, CancellationToken cancellationToken)
        {
            TaskCompletionSource<bool> cancellationTaskSource = new TaskCompletionSource<bool>();
            using (cancellationToken.Register(() => cancellationTaskSource.TrySetResult(true)))
            {
                if (task != await Task.WhenAny(task, cancellationTaskSource.Task))
                {
                    throw new OperationCanceledException(cancellationToken);
                }
            }

            return await task;
        }
    }
}

