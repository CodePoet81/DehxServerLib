namespace DehxServerLib.ServerMessaging;

public class MessageNewGame : BaseMessage
{
    public MessageNewGame()
    {
        messageType = MessageType.NewGame;
    }

}

