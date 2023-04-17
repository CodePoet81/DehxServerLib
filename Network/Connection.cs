using DehxServerLib.ServerMessaging;
using System.Net.Sockets;

namespace DehxServerLib.Network;

public class Connection
{
    public object ServerRef;
    private NetworkStream _stream;
    public TcpClient tcpClient;
    public int clientId;
    private DisconnectedCallback disconnectEvent;
    public string loginName;
    private readonly ServerMessageHandlerCallback ServerMessageEvent;
    private readonly ClientMessageHandlerCallback ClientMessageEvent;
    public Messaging messaging { get; set; }

    public Connection(TcpClient c, int clientid, DisconnectedCallback disconnectedCallback,
        ServerMessageHandlerCallback svrMsgHnd, object Server)
    {
        // this is a connection from server to client
        disconnectEvent = disconnectedCallback;
        ServerMessageEvent = svrMsgHnd;
        clientId = clientid;
        tcpClient = c;
        stream = c.GetStream();
        ServerRef = Server;

        Task.Run(TCPDataHandler).GetAwaiter().OnCompleted(() => { disconnectedCallback(clientid); });
    }

    public Connection(ClientMessageHandlerCallback clientMsgHnd)
    {
        // this is a connection from client to server
        this.tcpClient = new TcpClient();

        clientId = -1;
        ClientMessageEvent = clientMsgHnd;
    }


    public NetworkStream stream
    {
        get => _stream;
        set
        {
            _stream = value;
            messaging = new Messaging(_stream);
        }
    }


    public NetworkStream Stream => stream;

    public int ClientId
    {
        get => clientId;
        set => clientId = value;
    }

    public bool Connected => tcpClient.Connected;

    public void Connect(string host, int tcpport)
    {
        // this is a connection from client to server
        tcpClient = new TcpClient();
        tcpClient.Connect(host, tcpport);
        stream = tcpClient.GetStream();

        Task.Run(TCPDataHandler);

    }

    public void Close()
    {
        tcpClient.Close();
    }

    public async void TCPDataHandler()
    {

        BinaryReader br = new BinaryReader(stream);
        while (tcpClient.Connected)
        {
            if (stream.DataAvailable)
            {
                try
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
                    if (ClientMessageEvent != null)
                    {
                        ClientMessageEvent(buffer, clientId);
                    }
                    if (ServerMessageEvent != null)
                    {
                        ServerMessageEvent(buffer, clientId);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
        }

    }


}