namespace DehxServerLib.ServerMessaging;

public class MessageNewPlayer : BaseMessage
{
    public string loginName { get; set; }

    public MessageNewPlayer(string loginName)
    {
        messageType = MessageType.NewPlayer;
        this.loginName = loginName;
    }


}