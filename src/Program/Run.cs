// See https://aka.ms/new-console-template for more information

using Akka.IO;
using Akka.Streams;

namespace Program;

using Akka.Actor;
using Akka.Streams.Dsl;
using Tcp = Akka.Streams.Dsl.Tcp;


public class Run
{
    public ActorSystem Sys { get; set; }
    private ActorMaterializer Materializer { get; }

    private Run()
    {
        Materializer = Sys.Materializer();
    }
    public static void Main_(string[] argv)
    {
        var run = new Run();
        run.Start();
    }
    public void Start()
    {
        Source<Tcp.IncomingConnection, Task<Tcp.ServerBinding>> connections =
            Sys.TcpStream().Bind("127.0.0.1", 8888);

        connections.RunForeach(connection =>
        {
            Console.WriteLine($"New connection from: {connection.RemoteAddress}");

            var echo = Flow.Create<ByteString>()
                .Via(Framing.Delimiter(
                    ByteString.FromString("\n"),
                    maximumFrameLength: 256,
                    allowTruncation: true))
                .Select(c => c.ToString())
                .Select(c => c + "!!!\n")
                .Select(ByteString.FromString);

            connection.HandleWith(echo, Materializer);
        }, Materializer); 
    }
}
