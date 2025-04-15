using System.Net;
using Akka.Actor;
using Akka.IO;

namespace Program;

public class EchoServer : UntypedActor
{
    public EchoServer(int port)
    {
        TcpExtensions
            .Tcp(Context.System)
            .Tell(new Tcp.Bind(Self, new IPEndPoint(IPAddress.Any, port)));
    }

    protected override void OnReceive(object message)
    {
        switch (message)
        {
            case Tcp.Bound bound:
                Console.WriteLine("Listening on {0}", bound.LocalAddress);
                break;
            
            case Tcp.Connected:
            {
                var connection = Context.ActorOf(Props.Create(() => new EchoConnection(Sender)));
                Sender.Tell(new Tcp.Register(connection));
                break;
            }
            
            default:
                Unhandled(message);
                break;
        }
    }
}