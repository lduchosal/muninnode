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
using MuninNode.Commands;
using MuninNode.Plugins;
using MuninNode.SocketCreate;

namespace MuninNode;

/// <summary>
/// Provides an extensible base class with basic Munin-Node functionality.
/// </summary>
/// <seealso href="https://guide.munin-monitoring.org/en/latest/node/index.html">The Munin node</seealso>
public class MuninNode(
    ILogger<MuninNode> logger,
    MuninNodeConfiguration config,
    IPluginProvider pluginProvider,
    IAccessRule accessRule,
    ISocketCreator socketServer,
    IEnumerable<ICommand> commands,
    IDefaultCommand help)
    : IMuninNode, IDisposable, IAsyncDisposable
{
    private Encoding Encoding => Encoding.Default;
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

    protected virtual
#if SYSTEM_NET_SOCKETS_SOCKET_DISCONNECTASYNC_REUSESOCKET_CANCELLATIONTOKEN
async
#endif
        ValueTask DisposeAsyncCore()
    {
        try
        {
            if (Server?.Connected ?? false)
            {
#if SYSTEM_NET_SOCKETS_SOCKET_DISCONNECTASYNC_REUSESOCKET_CANCELLATIONTOKEN
await server.DisconnectAsync(reuseSocket: false).ConfigureAwait(false);
#else
                Server.Disconnect(reuseSocket: false);
#endif
            }
        }
        catch (SocketException)
        {
// swallow
        }

        Server?.Close();
        Server?.Dispose();

#if !SYSTEM_NET_SOCKETS_SOCKET_DISCONNECTASYNC_REUSESOCKET_CANCELLATIONTOKEN
        return default;
#endif
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
                if (cancellationToken.IsCancellationRequested)
                {
                    if (throwIfCancellationRequested)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                    }
                    else
                    {
                        return;
                    }
                }

                await AcceptSingleSessionAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException ex)
        {
            if (throwIfCancellationRequested || ex.CancellationToken != cancellationToken)
            {
                throw;
            }

            return;
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
        CancellationToken cancellationToken = default
    )
    {
        logger.LogInformation("accepting...");

        var client = await Server!
#if SYSTEM_NET_SOCKETS_SOCKET_ACCEPTASYNC_CANCELLATIONTOKEN
.AcceptAsync(cancellationToken: cancellationToken)
#else
            .AcceptAsync()
#endif
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
                await SendResponseAsync(
                    client,
                    Encoding,
                    [$"# munin node at {config.Hostname}"],
                    cancellationToken
                ).ConfigureAwait(false);
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
#pragma warning disable CA1031
            catch (Exception ex)
            {
                logger.LogCritical(
                    ex,
                    "[{RemoteEndPoint}] unexpected exception occured while sending banner",
                    remoteEndPoint
                );

                return;
            }
#pragma warning restore CA1031

            cancellationToken.ThrowIfCancellationRequested();

            logger.LogInformation("[{RemoteEndPoint}] session started; ID={SessionId}", remoteEndPoint, sessionId);

            try
            {
                if (pluginProvider.SessionCallback is not null)
                {
                    await pluginProvider.SessionCallback.ReportSessionStartedAsync(sessionId, cancellationToken)
                        .ConfigureAwait(false);
                }

                foreach (var plugin in pluginProvider.Plugins)
                {
                    if (plugin.SessionCallback is not null)
                    {
                        await plugin.SessionCallback.ReportSessionStartedAsync(sessionId, cancellationToken)
                            .ConfigureAwait(false);
                    }
                }

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
                foreach (var plugin in pluginProvider.Plugins)
                {
                    if (plugin.SessionCallback is not null)
                    {
                        await plugin.SessionCallback.ReportSessionClosedAsync(sessionId, cancellationToken)
                            .ConfigureAwait(false);
                    }
                }

                if (pluginProvider.SessionCallback is not null)
                {
                    await pluginProvider.SessionCallback.ReportSessionClosedAsync(sessionId, cancellationToken)
                        .ConfigureAwait(false);
                }
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
#if SYSTEM_SECURITY_CRYPTOGRAPHY_SHA1_HASHSIZEINBYTES
const int SHA1HashSizeInBytes = SHA1.HashSizeInBytes;
#else
        const int sha1HashSizeInBytes = 160 /*bits*/ / 8;
#endif

        var sessionIdentity = Encoding.ASCII.GetBytes($"{localEndPoint}\n{remoteEndPoint}\n{DateTimeOffset.Now:o}");

        Span<byte> sha1Hash = stackalloc byte[sha1HashSizeInBytes];

#pragma warning disable CA5350
#if SYSTEM_SECURITY_CRYPTOGRAPHY_SHA1_TRYHASHDATA
SHA1.TryHashData(sessionIdentity, sha1hash, out var bytesWrittenSHA1);
#else
        using var sha1 = SHA1.Create();

        sha1.TryComputeHash(sessionIdentity, sha1Hash, out var bytesWrittenSHA1);
#endif
#pragma warning restore CA5350

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
                    "[{RemoteEndPoint}] expected socket exception ({NumericSocketErrorCode} {SocketErrorCode})",
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
                        commandLine: line,
                        cancellationToken: cancellationToken
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
#pragma warning disable CA1031
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
#pragma warning restore CA1031

            reader.AdvanceTo(buffer.Start, buffer.End);

            if (result.IsCompleted)
            {
                break;
            }
        }

    }

    private static bool ExpectCommand(
        ReadOnlySequence<byte> commandLine,
        ReadOnlySpan<byte> expectedCommand,
        out ReadOnlySequence<byte> arguments
    )
    {
        arguments = default;

        var reader = new SequenceReader<byte>(commandLine);

        if (!reader.IsNext(expectedCommand, advancePast: true))
        {
            return false;
        }

        const byte space = (byte)' ';

        if (reader.Remaining == 0)
        {
            // <command> <EOL>
            arguments = default;
            return true;
        }
        else if (reader.IsNext(space, advancePast: true))
        {
            // <command> <SP> <arguments> <EOL>
#if SYSTEM_BUFFERS_SEQUENCEREADER_UNREADSEQUENCE
            arguments = reader.UnreadSequence;
#else
            arguments = reader.Sequence.Slice(reader.Position);
#endif
            return true;
        }

        return false;
    }
    private async ValueTask RespondToCommandAsync(
        Socket client,
        ReadOnlySequence<byte> commandLine,
        CancellationToken cancellationToken
    )
    {
        if (
            ExpectCommand(commandLine, "quit"u8, out _) 
            || ExpectCommand(commandLine, "."u8, out _) // quit short
        )
        {
            client.Close();
            return;
        }
        
        string[] result = [];
        bool matched = false;
        foreach (var command in commands)
        {
            if (ExpectCommand(commandLine, command.Name, out var args))
            {
                result = await command.ProcessAsync(args, cancellationToken);
                matched = true;
                break;
            }
        }
        if (!matched)
        {
            result = await help.ProcessAsync(ReadOnlySequence<byte>.Empty, cancellationToken);
        }
        
        await SendResponseAsync(
            client: client,
            encoding: Encoding,
            responseLines: result,
            cancellationToken: cancellationToken
        );
    }

    private static readonly ReadOnlyMemory<byte> EndOfLine = new[] { (byte)'\n' };

    private static async ValueTask SendResponseAsync(
        Socket client,
        Encoding encoding,
        IEnumerable<string> responseLines,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(responseLines);

        cancellationToken.ThrowIfCancellationRequested();

        foreach (var responseLine in responseLines)
        {
            var resp = encoding.GetBytes(responseLine);

            await client.SendAsync(
                buffer: resp,
                socketFlags: SocketFlags.None,
                cancellationToken: cancellationToken
            ).ConfigureAwait(false);

            await client.SendAsync(
                buffer: EndOfLine,
                socketFlags: SocketFlags.None,
                cancellationToken: cancellationToken
            ).ConfigureAwait(false);
        }
    }

    public async Task RunAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation($"starting");
        Server = socketServer.CreateServerSocket() ?? throw new InvalidOperationException("cannot start server");
        logger.LogInformation("started (end point: {LocalEndPoint})", Server.LocalEndPoint);
        await AcceptAsync(false, stoppingToken);
    }
}