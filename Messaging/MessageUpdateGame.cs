using System.Net.Sockets;

namespace DehxServerLib.ServerMessaging;

public class MessageUpdateGame<T> : BaseMessage
{
    public T GameState { get; set; }

    public MessageUpdateGame()
    {
        messageType = MessageType.UpdateGameState;
    }

    public MessageUpdateGame(T newGameState)
    {
        messageType = MessageType.UpdateGameState;
        this.GameState = newGameState;
    }

}