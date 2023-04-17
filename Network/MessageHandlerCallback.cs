
namespace DehxServerLib.Network;

public delegate void ServerMessageHandlerCallback(byte[] message, int clientId);
public delegate void ClientMessageHandlerCallback(byte[] message, int clientId);

public delegate void BroadcastCallback(dynamic newGameState);