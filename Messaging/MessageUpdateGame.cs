namespace DehxServerLib.ServerMessaging;

public class MessageUpdateGame : BaseMessage
{
    public dynamic GameState { get; set; }

    public MessageUpdateGame()
    {
        messageType = MessageType.UpdateGameState;
    }

    public MessageUpdateGame(dynamic newGameState)
    {
        messageType = MessageType.UpdateGameState;
        this.GameState = newGameState;
    }

}