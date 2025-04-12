using System.Buffers;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using MuninNode.AccessRules;

namespace MuninNode.Server;

public class SessionManager(
    ILogger<SessionManager> logger,
    IAccessRule accessRule,
    SocketListener socketListener,
    MuninProtocol protocol,
    CommunicationHandler communicationHandler
    )
{
    private static Encoding Encoding => Encoding.Default;

    private static string GenerateSessionId(EndPoint? localEndPoint, IPEndPoint remoteEndPoint)
    {
        var sessionIdentity = Encoding.ASCII.GetBytes($"{localEndPoint}\n{remoteEndPoint}\n{DateTimeOffset.Now:o}");
        Span<byte> sha1Hash = stackalloc byte[SHA1.HashSizeInBytes];
        SHA1.TryHashData(sessionIdentity, sha1Hash, out _);
        return Convert.ToBase64String(sha1Hash);
    }

    /// <summary>
    /// Starts accepting single session.
    /// The <see cref="ValueTask" /> this method returns will complete when the accepted session is closed or the cancellation requested by the <paramref name="cancellationToken" />.
    /// </summary>
    /// <param name="server"></param>
    /// <param name="cancellationToken">
    /// The <see cref="CancellationToken" /> to stop accepting sessions.
    /// </param>
    private async ValueTask AcceptSingleSessionAsync(
        CancellationToken cancellationToken
    )
    {
        logger.LogInformation("accepting...");
        
        var client = await socketListener
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

            var sessionId = GenerateSessionId(socketListener.LocalEndPoint, remoteEndPoint);
            cancellationToken.ThrowIfCancellationRequested();

            logger.LogDebug("[{RemoteEndPoint}] sending banner", remoteEndPoint);

            try
            {
                var banner = protocol.GetBanner();
                await communicationHandler.SendResponseAsync(
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
                    communicationHandler.ReceiveCommandAsync(client, remoteEndPoint, pipe.Writer, cancellationToken),
                    communicationHandler.ProcessCommandAsync(client, remoteEndPoint, pipe.Reader, cancellationToken)
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

    /// <summary>
    /// Starts accepting multiple sessions.
    /// The <see cref="ValueTask" /> this method returns will never complete unless the cancellation requested by the <paramref name="cancellationToken" />.
    /// </summary>
    /// <param name="server"></param>
    /// <param name="throwIfCancellationRequested">
    /// If <see langworkd="true" />, throws an <see cref="OperationCanceledException" /> on cancellation requested.
    /// If <see langworkd="false" />, completes the task without throwing an <see cref="OperationCanceledException" />.
    /// </param>
    /// <param name="cancellationToken">
    /// The <see cref="CancellationToken" /> to stop accepting sessions.
    /// </param>
    internal async ValueTask AcceptAsync(
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

    internal async ValueTask RespondToCommandAsync(
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
        
        await communicationHandler.SendResponseAsync(
            client: client,
            encoding: Encoding,
            cancellationToken: cancellationToken, 
            responseLines: result.Lines);
    }
}