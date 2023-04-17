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
}