using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;

namespace MuninNode.Server;
public class SocketListener : Socket
{
    private readonly Socket Server;
    private readonly ILogger<SocketListener> Logger;
    private readonly MuninNodeConfiguration Config;

    public SocketListener(ILogger<SocketListener> logger, MuninNodeConfiguration config)
    {
        Logger = logger;
        Config = config;
        Server = CreateServerSocket();
    }

    public new EndPoint? LocalEndPoint => Server.LocalEndPoint;

    internal Socket CreateServerSocket()
    {
        Logger.LogInformation("started (end point: {LocalEndPoint})", Server.LocalEndPoint);
        const int maxClients = 1;
        Socket? server = null;

        try
        {
            var endPoint = new IPEndPoint(
                address: Config.Listen,
                port: Config.Port
            );

            server = new Socket(
                endPoint.AddressFamily,
                SocketType.Stream,
                ProtocolType.Tcp
            );

            if (endPoint.AddressFamily == AddressFamily.InterNetworkV6 && Socket.OSSupportsIPv4)
            {
                server.DualMode = true;
            }

            server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            server.Bind(endPoint);
            server.Listen(maxClients);

            return server;
        }
        catch
        {
            server?.Dispose();
            throw;
        }
    }
    
    
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore().ConfigureAwait(false);

        Dispose(disposing: false);
        GC.SuppressFinalize(this);
    }

    private async ValueTask DisposeAsyncCore()
    {
        try
        {
            if (Server?.Connected ?? false)
            {
                await Server.DisconnectAsync(reuseSocket: false).ConfigureAwait(false);
            }
        }
        catch (SocketException)
        {
            // swallow
        }

        Server?.Close();
        Server?.Dispose();
    }

    private void Dispose(bool disposing)
    {
        if (!disposing)
        {
            return;
        }

        try
        {
            if (Server?.Connected ?? false)
            {
                Server.Disconnect(reuseSocket: false);
            }
        }
        catch (SocketException)
        {
            // swallow
        }

        Server?.Close();
        Server?.Dispose();
    }

    public async Task<Socket> AcceptAsync(CancellationToken cancellationToken)
    {
        return await Server.AcceptAsync(cancellationToken);
    }
}