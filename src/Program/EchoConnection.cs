using Akka.Actor;
using Akka.IO;
using Akka.Streams.Dsl;
using Tcp = Akka.IO.Tcp;

namespace Program;

public class EchoConnection(IActorRef connection) : UntypedActor
{
    protected override void OnReceive(object message)
    {
        if (message is Tcp.Connected connected)
        {
            var welcomeMessage = $"Welcome to: {connected.LocalAddress}, you are: {connected.RemoteAddress}!";
            var welcome = Source.Single(welcomeMessage);
            connection.Tell(Tcp.Write.Create(welcome));

        }

        else if (message is Tcp.Received received)
        {
            // if (received.Data[0] == 'x')
            //     Context.Stop(Self);
            // else
            //     connection.Tell(Tcp.Write.Create(received.Data));
            
            // server logic, parses incoming commands
            var commandParser = Flow.Create<string>()
                .TakeWhile(c => c != "quit" && c != ".")
                ;

            var serverLogic = Flow.Create<ByteString>()
                .Via(Framing.Delimiter(
                    ByteString.FromString("\n"),
                    maximumFrameLength: 128,
                    allowTruncation: true))
                .Select(c => c.ToString())
                .Select(command =>
                {
                    serverProbe.Tell(command);
                    return command;
                })
                .Via(commandParser)
                .Merge(welcome)
                .Select(c => c + "\n")
                .Select(ByteString.FromString);

            connection.HandleWith(serverLogic, Materializer);
            
            
        }
        else Unhandled(message);
        
    }
}