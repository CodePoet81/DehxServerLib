using System.Net.Sockets;

namespace DehxServerLib.ServerMessaging;

public class BaseMessage
{
    public byte messageType { get; set; }

    public void Send(Stream stream)
    {
        var sm = Messaging.SerializeMessage(this);
        if (stream.CanWrite)
        {
            stream.Write(sm, 0, sm.Length);
        }
    }

    public void Send<T1>(NetworkStream stream)
    {
        var sm = Messaging.SerializeMessage<T1>(this);
        if (stream.CanWrite)
        {
            stream.Write(sm, 0, sm.Length);
        }
    }
}