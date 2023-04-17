﻿using System.Net;
using System.Net.Sockets;

namespace LGSCommon
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

            // Start the TCP server
            tcpListener = new TcpListener(IPAddress.Any, tcpPort);
            tcpListener.Start();
            Console.WriteLine("TCP server started on port {0}", tcpPort);

            // Start the UDP server
            udpServer = new UdpClient(udpPort);
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
                IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
                try
                {
                    byte[] data = udpServer.Receive(ref remoteEP);
                    if (!udpClients.Contains(remoteEP))
                    {
                        udpClients.Add(remoteEP);
                        Console.WriteLine("UDP client connected from {0}", remoteEP);
                    }
                    HandleUdpPacket(data, remoteEP);
                }
                catch (SocketException se)
                {
                    udpClients.Remove(remoteEP);
                    Console.WriteLine("UDP Client Disconnected {0}", remoteEP);
                }

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

                        byte mType = br.ReadByte();
                        byte[] mLenb = br.ReadBytes(4);
                        int mLen = BitConverter.ToInt32(mLenb, 0);
                        byte[] buffer = new byte[mLen + 5];

                        buffer[0] = mType;
                        Array.Copy(mLenb, 0, buffer, 1, 4);

                        int numberOfBytesRead = 5 + stream.Read(buffer, 5, mLen);
                        while (numberOfBytesRead < (mLen + 5))
                        {
                            numberOfBytesRead += stream.Read(buffer, numberOfBytesRead, mLen - numberOfBytesRead);
                        }

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
            foreach (IPEndPoint otherClient in udpClients)
            {
                udpServer.Send(data, data.Length, otherClient);
            }
        }
        public void SendTcpBroadcast(byte[] data)
        {
            foreach (TcpClient otherClient in tcpClients)
            {
                otherClient.GetStream().Write(data, 0, data.Length);
            }
        }

        public void SendUdp(byte[] data, IPEndPoint remoteEP)
        {
            udpServer.Send(data, data.Length, remoteEP);
        }


        public void SendTcp(byte[] data, IPEndPoint remoteEP)
        {
            udpServer.Send(data, data.Length, remoteEP);
        }
    }

}
