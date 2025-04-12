using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging;

namespace MuninNode.Server;

public class CommunicationHandler(
    ILogger<CommunicationHandler> logger,
    SessionManager sessionManager)
{
    internal async Task ReceiveCommandAsync(
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

    internal async Task ProcessCommandAsync(
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
                    await sessionManager.RespondToCommandAsync(
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
    
    
    internal async ValueTask SendResponseAsync(
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
}