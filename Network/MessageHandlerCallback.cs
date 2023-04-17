
namespace DehxServerLib.Network;

public delegate void MessageHandlerCallback(byte[] message, int clientId);

public delegate void BroadcastCallback(dynamic newGameState);