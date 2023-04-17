using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace DehxServerLib.ServerMessaging;

public class Messaging
{
    public readonly NetworkStream stream;
    private static long MsgSeqId = 0;

    public static dynamic DeSerializeMessage<T>(byte[] memStream)
    {
        var messageType = memStream[0];
        var msgLen = BitConverter.ToInt32(memStream, 1);

        byte[] data = new byte[msgLen];
        Array.Copy(memStream, 5, data, 0, msgLen);
        var m = Encoding.ASCII.GetString(data);

        return JsonSerializer.Deserialize(m, MessageType.GetType<T>(messageType), new JsonSerializerOptions { IncludeFields = true });

    }

    public static dynamic DeSerializeMessage(byte[] memStream)
    {
        var messageType = memStream[0];
        var msgLen = BitConverter.ToInt32(memStream, 1);

        byte[] data = new byte[msgLen];
        Array.Copy(memStream, 5, data, 0, msgLen);
        var m = Encoding.ASCII.GetString(data);

        return JsonSerializer.Deserialize(m, MessageType.GetType(messageType), new JsonSerializerOptions { IncludeFields = true });

    }
    public static byte[] SerializeMessage(dynamic message)
    {
        byte[] msgStr = Encoding.ASCII.GetBytes(JsonSerializer.Serialize(message, MessageType.GetType(message.messageType), new JsonSerializerOptions { IncludeFields = true })); ;
        byte[] data = new byte[msgStr.Length + 5];
        data[0] = message.messageType;
        Array.Copy(BitConverter.GetBytes(msgStr.Length), 0, data, 1, 4);
        Array.Copy(msgStr, 0, data, 5, msgStr.Length);
        return data;
    }

    public static byte[] SerializeMessage<T>(dynamic message)
    {
        byte[] msgStr = Encoding.ASCII.GetBytes(JsonSerializer.Serialize(message, MessageType.GetType<T>(message.messageType), new JsonSerializerOptions { IncludeFields = true })); ;
        byte[] data = new byte[msgStr.Length + 5];
        data[0] = message.messageType;
        Array.Copy(BitConverter.GetBytes(msgStr.Length), 0, data, 1, 4);
        Array.Copy(msgStr, 0, data, 5, msgStr.Length);
        return data;
    }

    public Messaging(NetworkStream stream)
    {
        this.stream = stream;
    }

}