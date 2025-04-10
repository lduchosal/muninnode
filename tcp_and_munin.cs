/// <summary>
/// Provides an extensible base class with basic Munin-Node functionality.
/// </summary>
/// <seealso href="https://guide.munin-monitoring.org/en/latest/node/index.html">The Munin node</seealso>
public class MuninNode(
    ILogger<MuninNode> logger,
    MuninNodeConfiguration config,
    IPluginProvider pluginProvider,
    IAccessRule accessRule,
    IEnumerable<ICommand> commands,
    IDefaultCommand help)
    : IMuninNode, IDisposable, IAsyncDisposable
{
    private static Encoding Encoding => Encoding.Default;
    private Socket? Server;
    public void Dispose()

    public async ValueTask DisposeAsync()
    protected virtual async ValueTask DisposeAsyncCore()
    {
    protected virtual void Dispose(bool disposing)
    {
        
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
    private static string GenerateSessionId(EndPoint? localEndPoint, IPEndPoint remoteEndPoint)
    

    private async Task ReceiveCommandAsync(
        Socket socket,
        IPEndPoint remoteEndPoint,
        PipeWriter writer,
        CancellationToken cancellationToken
    )

    private async Task ProcessCommandAsync(
        Socket socket,
        IPEndPoint remoteEndPoint,
        PipeReader reader,
        CancellationToken cancellationToken
    )

    private async ValueTask RespondToCommandAsync(
        Socket client,
        ReadOnlySequence<byte> commandLine,
        CancellationToken cancellationToken
    )
    
    
    private static async ValueTask SendResponseAsync(Socket client,
        Encoding encoding,
        CancellationToken cancellationToken,
        params string[] responseLines)
        
    private Socket CreateServerSocket()
    
    public async Task RunAsync(CancellationToken stoppingToken)
}