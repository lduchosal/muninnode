using System.Net.Sockets;

namespace MuninNode.SocketCreate;

public interface ISocketCreator {
  Socket CreateServerSocket();
}
