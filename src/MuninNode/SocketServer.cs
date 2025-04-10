// SPDX-FileCopyrightText: 2021 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

using System.Buffers;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using MuninNode.AccessRules;

namespace MuninNode;

/// <summary>
/// Provides an extensible base class with basic Munin-Node functionality.
/// </summary>
/// <seealso href="https://guide.munin-monitoring.org/en/latest/node/index.html">The Munin node</seealso>
public class SocketServer(
    ILogger<SocketServer> logger,
    MuninProtocol protocol,
    MuninNodeConfiguration config,
    IAccessRule accessRule)
    : IMuninNode, IDisposable, IAsyncDisposable
{
    private static Encoding Encoding => Encoding.Default;
    private Socket? Server;
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
    protected virtual async ValueTask DisposeAsyncCore()
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
    protected virtual void Dispose(bool disposing)
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

    /// <summary>
    /// Starts accepting multiple sessions.
    /// The <see cref="ValueTask" /> this method returns will never complete unless the cancellation requested by the <paramref name="cancellationToken" />.
    /// </summary>
    /// <param name="throwIfCancellationRequested">
    /// If <see langworkd="true" />, throws an <see cref="OperationCanceledException" /> on cancellation requested.
    /// If <see langworkd="false" />, completes the task without throwing an <see cref="OperationCanceledException" />.
    /// </param>
    /// <param name="cancellationToken">
    /// The <see cref="CancellationToken" /> to stop accepting sessions.
    /// </param>
    private async ValueTask AcceptAsync(
        bool throwIfCancellationRequested,
        CancellationToken cancellationToken
    )
    {
        try
        {
            for (;;)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await AcceptSingleSessionAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException ex)
        {
            if (throwIfCancellationRequested || ex.CancellationToken != cancellationToken)
            {
                throw;
            }
        }
    }

    /// <summary>
    /// Starts accepting single session.
    /// The <see cref="ValueTask" /> this method returns will complete when the accepted session is closed or the cancellation requested by the <paramref name="cancellationToken" />.
    /// </summary>
    /// <param name="cancellationToken">
    /// The <see cref="CancellationToken" /> to stop accepting sessions.
    /// </param>
    private async ValueTask AcceptSingleSessionAsync(
        CancellationToken cancellationToken
    )
    {
        logger.LogInformation("accepting...");

        var client = await Server!
            .AcceptAsync(cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        IPEndPoint? remoteEndPoint = null;

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            remoteEndPoint = client.RemoteEndPoint as IPEndPoint;

            if (remoteEndPoint is null)
            {
                logger.LogWarning(
                    "cannot accept {RemoteEndPoint} ({RemoteEndPointAddressFamily})",
                    client.RemoteEndPoint?.ToString() ?? "(null)",
                    client.RemoteEndPoint?.AddressFamily
                );
                return;
            }

            if (!accessRule.IsAcceptable(remoteEndPoint))
            {
                logger.LogWarning("access refused: {RemoteEndPoint}", remoteEndPoint);
                return;
            }

            var sessionId = GenerateSessionId(Server.LocalEndPoint, remoteEndPoint);
            cancellationToken.ThrowIfCancellationRequested();

            logger.LogDebug("[{RemoteEndPoint}] sending banner", remoteEndPoint);

            try
            {
                var banner = protocol.GetBanner();
                await SendResponseAsync(
                    client,
                    Encoding,
                    cancellationToken, banner).ConfigureAwait(false);
            }
            catch (SocketException ex) when (
                ex.SocketErrorCode is
                    SocketError.Shutdown or // EPIPE (32)
                    SocketError.ConnectionAborted or // WSAECONNABORTED (10053)
                    SocketError.OperationAborted or // ECANCELED (125)
                    SocketError.ConnectionReset // ECONNRESET (104)
            )
            {
                logger.LogWarning(
                    "[{RemoteEndPoint}] client closed session while sending banner",
                    remoteEndPoint
                );

                return;
            }
            catch (Exception ex)
            {
                logger.LogCritical(
                    ex,
                    "[{RemoteEndPoint}] unexpected exception occured while sending banner",
                    remoteEndPoint
                );

                return;
            }

            cancellationToken.ThrowIfCancellationRequested();
            logger.LogInformation("[{RemoteEndPoint}] session started; ID={SessionId}", remoteEndPoint, sessionId);

            try
            {
                await protocol.SessionStartedAsync(sessionId, cancellationToken);

                // https://docs.microsoft.com/ja-jp/dotnet/standard/io/pipelines
                var pipe = new Pipe();
                await Task.WhenAll(
                    ReceiveCommandAsync(client, remoteEndPoint, pipe.Writer, cancellationToken),
                    ProcessCommandAsync(client, remoteEndPoint, pipe.Reader, cancellationToken)
                ).ConfigureAwait(false);

                logger.LogInformation("[{RemoteEndPoint}] session closed; ID={SessionId}", remoteEndPoint, sessionId);
            }
            finally
            {
                await protocol.SessionClosedAsync(sessionId, cancellationToken);
            }
        }
        finally
        {
            client.Close();
            logger.LogInformation("[{RemoteEndPoint}] connection closed", remoteEndPoint);
        }
    }

    private static string GenerateSessionId(EndPoint? localEndPoint, IPEndPoint remoteEndPoint)
    {
        var sessionIdentity = Encoding.ASCII.GetBytes($"{localEndPoint}\n{remoteEndPoint}\n{DateTimeOffset.Now:o}");
        Span<byte> sha1Hash = stackalloc byte[SHA1.HashSizeInBytes];
        SHA1.TryHashData(sessionIdentity, sha1Hash, out _);
        return Convert.ToBase64String(sha1Hash);
    }

    private async Task ReceiveCommandAsync(
        Socket socket,
        IPEndPoint remoteEndPoint,
        PipeWriter writer,
        CancellationToken cancellationToken
    )
    {
        const int minimumBufferSize = 256;

        for (;;)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var memory = writer.GetMemory(minimumBufferSize);

            try
            {
                if (!socket.Connected)
                {
                    break;
                }

                var bytesRead = await socket.ReceiveAsync(
                    buffer: memory,
                    socketFlags: SocketFlags.None,
                    cancellationToken: cancellationToken
                ).ConfigureAwait(false);

                if (bytesRead == 0)
                {
                    break;
                }

                writer.Advance(bytesRead);
            }
            catch (SocketException ex) when (
                ex.SocketErrorCode is
                    SocketError.OperationAborted or // ECANCELED (125)
                    SocketError.ConnectionReset // ECONNRESET (104)
            )
            {
                logger.LogInformation(
                    "[{RemoteEndPoint}] expected socket error ({NumericSocketErrorCode} {SocketErrorCode})",
                    remoteEndPoint,
                    (int)ex.SocketErrorCode,
                    ex.SocketErrorCode
                );
                break; // expected exception
            }
            catch (ObjectDisposedException)
            {
                logger.LogInformation(
                    "[{RemoteEndPoint}] socket has been disposed",
                    remoteEndPoint
                );
                break; // expected exception
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation(
                    "[{RemoteEndPoint}] operation canceled",
                    remoteEndPoint
                );
                throw;
            }
            catch (Exception ex)
            {
                logger.LogCritical(
                    ex,
                    "[{RemoteEndPoint}] unexpected exception while receiving",
                    remoteEndPoint
                );
                break;
            }

            var result = await writer.FlushAsync(
                cancellationToken: cancellationToken
            ).ConfigureAwait(false);

            if (result.IsCompleted)
            {
                break;
            }
        }

        await writer.CompleteAsync().ConfigureAwait(false);
    }

    private async Task ProcessCommandAsync(
        Socket socket,
        IPEndPoint remoteEndPoint,
        PipeReader reader,
        CancellationToken cancellationToken
    )
    {
        for (;;)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var result = await reader.ReadAsync(
                cancellationToken: cancellationToken
            ).ConfigureAwait(false);
            var buffer = result.Buffer;

            try
            {
                while (Sequence.TryReadLine(ref buffer, out var line))
                {
                    await RespondToCommandAsync(
                        client: socket,
                        cancellationToken: cancellationToken,
                        commandLine: line
                    ).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation(
                    "[{RemoteEndPoint}] operation canceled",
                    remoteEndPoint
                );
                throw;
            }
            catch (Exception ex)
            {
                logger.LogCritical(
                    ex,
                    "[{RemoteEndPoint}] unexpected exception while processing command",
                    remoteEndPoint
                );

                if (socket.Connected)
                {
                    socket.Close();
                }
                break;
            }

            reader.AdvanceTo(buffer.Start, buffer.End);
            if (result.IsCompleted)
            {
                break;
            }
        }
    }

    private async ValueTask RespondToCommandAsync(
        Socket client,
        ReadOnlySequence<byte> commandLine,
        CancellationToken cancellationToken
    )
    {
        var result = await protocol.HandleCommandAsync(commandLine, cancellationToken);
        if (result.Status == Status.Quit)
        {
            client.Close();
            return;
        }
        
        await SendResponseAsync(
            client: client,
            encoding: Encoding,
            cancellationToken: cancellationToken, 
            responseLines: result.Lines);
    }
    
    private static async ValueTask SendResponseAsync(
        Socket client,
        Encoding encoding,
        CancellationToken cancellationToken,
        List<string> responseLines)
    {
        cancellationToken.ThrowIfCancellationRequested();

        foreach (var responseLine in responseLines)
        {
            var resp = encoding.GetBytes(responseLine);
            await client.SendAsync(
                buffer: resp,
                socketFlags: SocketFlags.None,
                cancellationToken: cancellationToken
            ).ConfigureAwait(false);
        }
    }
    private Socket CreateServerSocket()
    {
        const int MaxClients = 1;
        Socket? server = null;

        try
        {
            var endPoint = new IPEndPoint(
                address: config.Listen,
                port: config.Port
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
            server.Listen(MaxClients);

            return server;
        }
        catch
        {
            server?.Dispose();
            throw;
        }
    }
    public async Task RunAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation($"starting");
        Server = CreateServerSocket() ?? throw new InvalidOperationException("cannot start server");
        logger.LogInformation("started (end point: {LocalEndPoint})", Server.LocalEndPoint);
        await AcceptAsync(false, stoppingToken);
    }
}