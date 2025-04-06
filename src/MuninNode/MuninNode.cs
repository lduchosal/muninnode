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
public class MuninNode : IMuninNode, IDisposable, IAsyncDisposable
{
    private MuninNodeConfiguration Config { get; set; }
    private ISocketCreator SocketServer { get; set; }
    private IAccessRule AccessRule { get; }
    private IPluginProvider PluginProvider { get; }
    private Encoding Encoding => Encoding.Default;
    private ILogger<MuninNode> Logger { get; }
    
    private Socket? Server;

    private EndPoint LocalEndPoint => Server?.LocalEndPoint ??
                                      throw new InvalidOperationException("not yet bound or already disposed");

    private ICommand ListCommand { get; }
    private ICommand HelpCommand { get; }
    private ICommand VersionCommand { get; }
    private ICommand CapCommand { get; }
    private ICommand NodeCommand { get; }
    private ICommand FetchCommand { get; }
    private ICommand ConfigCommand { get; }
    
    public MuninNode(
        ILogger<MuninNode> logger,
        MuninNodeConfiguration config,
        IPluginProvider pluginProvider,
        IAccessRule accessRule,
        ISocketCreator socketServer, 
        ListCommand listCommand, 
        HelpCommand helpCommand, 
        VersionCommand versionCommand, 
        CapCommand capCommand, 
        NodeCommand nodeCommand, 
        FetchCommand fetchCommand, 
        ConfigCommand configCommand)
    {
        PluginProvider = pluginProvider;
        AccessRule = accessRule;
        SocketServer = socketServer;
        ListCommand = listCommand;
        HelpCommand = helpCommand;
        VersionCommand = versionCommand;
        CapCommand = capCommand;
        NodeCommand = nodeCommand;
        FetchCommand = fetchCommand;
        ConfigCommand = configCommand;
        Config = config;
        Logger = logger;
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
            return;

        try
        {
            if (Server?.Connected ?? false)
                Server.Disconnect(reuseSocket: false);
        }
        catch (SocketException)
        {
// swallow
        }

        Server?.Close();
        Server?.Dispose();
    }

    protected void ThrowIfPluginProviderIsNull()
    {
        if (PluginProvider is null)
            throw new InvalidOperationException($"{nameof(PluginProvider)} cannot be null");
    }
    
    public void Start()
    {
        Logger.LogInformation($"starting");

        Server = SocketServer.CreateServerSocket() ?? throw new InvalidOperationException("cannot start server");

        Logger.LogInformation("started (end point: {LocalEndPoint})", Server.LocalEndPoint);
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
    public async ValueTask AcceptAsync(
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
                        cancellationToken.ThrowIfCancellationRequested();
                    else
                        return;
                }

                await AcceptSingleSessionAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException ex)
        {
            if (throwIfCancellationRequested || ex.CancellationToken != cancellationToken)
                throw;

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
    public async ValueTask AcceptSingleSessionAsync(
        CancellationToken cancellationToken = default
    )
    {
        ThrowIfPluginProviderIsNull();

        Logger.LogInformation("accepting...");

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
                Logger.LogWarning(
                    "cannot accept {RemoteEndPoint} ({RemoteEndPointAddressFamily})",
                    client.RemoteEndPoint?.ToString() ?? "(null)",
                    client.RemoteEndPoint?.AddressFamily
                );
                return;
            }

            if (!AccessRule.IsAcceptable(remoteEndPoint))
            {
                Logger.LogWarning("access refused: {RemoteEndPoint}", remoteEndPoint);
                return;
            }

            var sessionId = GenerateSessionId(Server.LocalEndPoint, remoteEndPoint);

            cancellationToken.ThrowIfCancellationRequested();

            Logger.LogDebug("[{RemoteEndPoint}] sending banner", remoteEndPoint);

            try
            {
                await SendResponseAsync(
                    client,
                    Encoding,
                    $"# munin node at {Config.Hostname}",
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
                Logger.LogWarning(
                    "[{RemoteEndPoint}] client closed session while sending banner",
                    remoteEndPoint
                );

                return;
            }
#pragma warning disable CA1031
            catch (Exception ex)
            {
                Logger.LogCritical(
                    ex,
                    "[{RemoteEndPoint}] unexpected exception occured while sending banner",
                    remoteEndPoint
                );

                return;
            }
#pragma warning restore CA1031

            cancellationToken.ThrowIfCancellationRequested();

            Logger.LogInformation("[{RemoteEndPoint}] session started; ID={SessionId}", remoteEndPoint, sessionId);

            try
            {
                if (PluginProvider.SessionCallback is not null)
                    await PluginProvider.SessionCallback.ReportSessionStartedAsync(sessionId, cancellationToken)
                        .ConfigureAwait(false);

                foreach (var plugin in PluginProvider.Plugins)
                {
                    if (plugin.SessionCallback is not null)
                        await plugin.SessionCallback.ReportSessionStartedAsync(sessionId, cancellationToken)
                            .ConfigureAwait(false);
                }

// https://docs.microsoft.com/ja-jp/dotnet/standard/io/pipelines
                var pipe = new Pipe();

                await Task.WhenAll(
                    ReceiveCommandAsync(client, remoteEndPoint, pipe.Writer, cancellationToken),
                    ProcessCommandAsync(client, remoteEndPoint, pipe.Reader, cancellationToken)
                ).ConfigureAwait(false);

                Logger.LogInformation("[{RemoteEndPoint}] session closed; ID={SessionId}", remoteEndPoint, sessionId);
            }
            finally
            {
                foreach (var plugin in PluginProvider.Plugins)
                {
                    if (plugin.SessionCallback is not null)
                        await plugin.SessionCallback.ReportSessionClosedAsync(sessionId, cancellationToken)
                            .ConfigureAwait(false);
                }

                if (PluginProvider.SessionCallback is not null)
                    await PluginProvider.SessionCallback.ReportSessionClosedAsync(sessionId, cancellationToken)
                        .ConfigureAwait(false);
            }
        }
        finally
        {
            client.Close();

            Logger.LogInformation("[{RemoteEndPoint}] connection closed", remoteEndPoint);
        }
    }

    private static string GenerateSessionId(EndPoint? localEndPoint, IPEndPoint remoteEndPoint)
    {
#if SYSTEM_SECURITY_CRYPTOGRAPHY_SHA1_HASHSIZEINBYTES
const int SHA1HashSizeInBytes = SHA1.HashSizeInBytes;
#else
        const int SHA1HashSizeInBytes = 160 /*bits*/ / 8;
#endif

        var sessionIdentity = Encoding.ASCII.GetBytes($"{localEndPoint}\n{remoteEndPoint}\n{DateTimeOffset.Now:o}");

        Span<byte> sha1hash = stackalloc byte[SHA1HashSizeInBytes];

#pragma warning disable CA5350
#if SYSTEM_SECURITY_CRYPTOGRAPHY_SHA1_TRYHASHDATA
SHA1.TryHashData(sessionIdentity, sha1hash, out var bytesWrittenSHA1);
#else
        using var sha1 = SHA1.Create();

        sha1.TryComputeHash(sessionIdentity, sha1hash, out var bytesWrittenSHA1);
#endif
#pragma warning restore CA5350

        return Convert.ToBase64String(sha1hash);
    }

    private async Task ReceiveCommandAsync(
        Socket socket,
        IPEndPoint remoteEndPoint,
        PipeWriter writer,
        CancellationToken cancellationToken
    )
    {
        const int MinimumBufferSize = 256;

        for (;;)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var memory = writer.GetMemory(MinimumBufferSize);

            try
            {
                if (!socket.Connected)
                    break;

                var bytesRead = await socket.ReceiveAsync(
                    buffer: memory,
                    socketFlags: SocketFlags.None,
                    cancellationToken: cancellationToken
                ).ConfigureAwait(false);

                if (bytesRead == 0)
                    break;

                writer.Advance(bytesRead);
            }
            catch (SocketException ex) when (
                ex.SocketErrorCode is
                    SocketError.OperationAborted or // ECANCELED (125)
                    SocketError.ConnectionReset // ECONNRESET (104)
            )
            {
                Logger.LogInformation(
                    "[{RemoteEndPoint}] expected socket exception ({NumericSocketErrorCode} {SocketErrorCode})",
                    remoteEndPoint,
                    (int)ex.SocketErrorCode,
                    ex.SocketErrorCode
                );
                break; // expected exception
            }
            catch (ObjectDisposedException)
            {
                Logger.LogInformation(
                    "[{RemoteEndPoint}] socket has been disposed",
                    remoteEndPoint
                );
                break; // expected exception
            }
            catch (OperationCanceledException)
            {
                Logger.LogInformation(
                    "[{RemoteEndPoint}] operation canceled",
                    remoteEndPoint
                );
                throw;
            }
#pragma warning disable CA1031
            catch (Exception ex)
            {
                Logger.LogCritical(
                    ex,
                    "[{RemoteEndPoint}] unexpected exception while receiving",
                    remoteEndPoint
                );
                break;
            }
#pragma warning restore CA1031

            var result = await writer.FlushAsync(
                cancellationToken: cancellationToken
            ).ConfigureAwait(false);

            if (result.IsCompleted)
                break;
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
                while (TryReadLine(ref buffer, out var line))
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
                Logger.LogInformation(
                    "[{RemoteEndPoint}] operation canceled",
                    remoteEndPoint
                );
                throw;
            }
#pragma warning disable CA1031
            catch (Exception ex)
            {
                Logger.LogCritical(
                    ex,
                    "[{RemoteEndPoint}] unexpected exception while processing command",
                    remoteEndPoint
                );

                if (socket.Connected)
                    socket.Close();
                break;
            }
#pragma warning restore CA1031

            reader.AdvanceTo(buffer.Start, buffer.End);

            if (result.IsCompleted)
                break;
        }

        static bool TryReadLine(ref ReadOnlySequence<byte> buffer, out ReadOnlySequence<byte> line)
        {
            var reader = new SequenceReader<byte>(buffer);
            const byte LF = (byte)'\n';

            if (
                !reader.TryReadTo(out line, delimiter: "\r\n"u8, advancePastDelimiter: true) &&
                !reader.TryReadTo(out line, delimiter: LF, advancePastDelimiter: true)
            )
            {
                line = default;
                return false;
            }

#if SYSTEM_BUFFERS_SEQUENCEREADER_UNREADSEQUENCE
buffer = reader.UnreadSequence;
#else
            buffer = reader.Sequence.Slice(reader.Position);
#endif

            return true;
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
            return false;

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

    private static readonly byte CommandQuitShort = (byte)'.';

    private async ValueTask RespondToCommandAsync(
        Socket client,
        ReadOnlySequence<byte> commandLine,
        CancellationToken cancellationToken
    )
    {

        string[] result = [];
        if (ExpectCommand(commandLine, "fetch"u8, out var fetchArguments))
        {
            result = await FetchCommand.ProcessAsync(fetchArguments, cancellationToken);
        }
        else if (ExpectCommand(commandLine, "nodes"u8, out var nodeargs))
        {
            result = await NodeCommand.ProcessAsync(nodeargs, cancellationToken);
        }
        else if (ExpectCommand(commandLine, "list"u8, out var listargs))
        {
            result = await ListCommand.ProcessAsync(listargs, cancellationToken);
        }
        else if (ExpectCommand(commandLine, "config"u8, out var configArguments))
        {
            result = await ConfigCommand.ProcessAsync(configArguments, cancellationToken);
        }
        else if (
            ExpectCommand(commandLine, "quit"u8, out _) ||
            (commandLine.Length == 1 && commandLine.FirstSpan[0] == CommandQuitShort)
        )
        {
            client.Close();
#if SYSTEM_THREADING_TASKS_VALUETASK_COMPLETEDTASK
return ValueTask.CompletedTask;
#else
            return;
#endif
        }
        else if (ExpectCommand(commandLine, "cap"u8, out var capArguments))
        {
            result = await CapCommand.ProcessAsync(capArguments, cancellationToken);
        }
        else if (ExpectCommand(commandLine, "version"u8, out var versionargs))
        {
            result = await VersionCommand.ProcessAsync(versionargs, cancellationToken);
        }
        else
        {
            result = await HelpCommand.ProcessAsync(ReadOnlySequence<byte>.Empty, cancellationToken);
        }
        
        await SendResponseAsync(
            client: client,
            encoding: Encoding,
            responseLines: result,
            cancellationToken: cancellationToken
        );

    }

#pragma warning disable IDE0230
    private static readonly ReadOnlyMemory<byte> EndOfLine = new[] { (byte)'\n' };
#pragma warning restore IDE0230


    private static ValueTask SendResponseAsync(
        Socket client,
        Encoding encoding,
        string responseLine,
        CancellationToken cancellationToken
    )
        => SendResponseAsync(
            client: client,
            encoding: encoding,
            responseLines: Enumerable.Repeat(responseLine, 1),
            cancellationToken: cancellationToken
        );

    private static async ValueTask SendResponseAsync(
        Socket client,
        Encoding encoding,
        IEnumerable<string> responseLines,
        CancellationToken cancellationToken
    )
    {
        if (responseLines == null)
            throw new ArgumentNullException(nameof(responseLines));

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
        Start();
        await AcceptAsync(false, stoppingToken);
    }
}