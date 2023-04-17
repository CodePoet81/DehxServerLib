

namespace DehxServerLib.ServerMessaging;

public static class MessageType
{
    public const byte NewGame = 1;
    public const byte NewPlayer = 4;
    public const byte UpdateGameState = 5;


    public static Type GetType(byte messageType)
    {

        switch (messageType)
        {
            case NewGame:
                return typeof(MessageNewGame);
            case NewPlayer:
                return typeof(MessageNewPlayer);
            case UpdateGameState:
                return typeof(MessageUpdateGame);
            default:
                throw new Exception($"Message Type Unkown for Message Type:{messageType}");
        }
    }
}