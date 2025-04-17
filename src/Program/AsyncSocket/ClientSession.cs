using System.Net.Sockets;
using System.Text;

namespace Program.AsyncSocket;

class ClientSession
{
    public Guid Id { get; }
    private readonly Socket _socket;
    private readonly char _delimiter;
    private readonly byte[] _receiveBuffer;
    private readonly SocketAsyncEventArgsPool _argsPool;
    private readonly StringBuilder _stringBuffer;
    private CancellationTokenSource Cts { get; set; } = new();
    private bool _isRunning;
    private readonly int _maxBufferSizeWithoutDelimiter;

    public event EventHandler<string> MessageReceived = delegate { };
    public event EventHandler<Guid> Disconnected = delegate { };

    public ClientSession(Guid id, Socket socket, char delimiter, int bufferSize, SocketAsyncEventArgsPool argsPool)
    {
        Id = id;
        _socket = socket;
        _delimiter = delimiter;
        _receiveBuffer = new byte[bufferSize];
        _argsPool = argsPool;
        _maxBufferSizeWithoutDelimiter = bufferSize; // Max size allowed without finding a delimiter
        _stringBuffer = new();
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        Cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _isRunning = true;
            
        try
        {
            await ReceiveLoopAsync(Cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation is requested
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in client {Id}: {ex.Message}");
        }
        finally
        {
            await StopAsync();
        }
    }

    public async Task StopAsync()
    {
        if (!_isRunning) return;
            
        _isRunning = false;
        await Cts.CancelAsync();
            
        try
        {
            _socket.Shutdown(SocketShutdown.Both);
        }
        catch { /* Ignore shutdown errors */ }
            
        _socket.Close();
            
        Disconnected.Invoke(this, Id);
            
        await Task.CompletedTask;
    }

    private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
    {
        var args = _argsPool.Get();
        args.SetBuffer(_receiveBuffer, 0, _receiveBuffer.Length);
            
        try
        {
            while (_isRunning && !cancellationToken.IsCancellationRequested)
            {
                var tcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
                    
                args.Completed += OnReceiveCompleted;
                args.UserToken = tcs;
                    
                bool isPending = _socket.ReceiveAsync(args);
                if (!isPending)
                {
                    OnReceiveCompleted(_socket, args);
                }
                    
                int bytesRead = await tcs.Task;
                if (bytesRead == 0)
                {
                    // Connection closed gracefully
                    break;
                }
                    
                string receivedText = Encoding.UTF8.GetString(_receiveBuffer, 0, bytesRead);
                _stringBuffer.Append(receivedText);
                    
                // Check if buffer exceeds limit without finding a delimiter
                if (_stringBuffer.Length > _maxBufferSizeWithoutDelimiter && 
                    _stringBuffer.ToString().IndexOf(_delimiter) == -1)
                {
                    Console.WriteLine($"Client {Id}: Buffer exceeded maximum size without delimiter. Disconnecting.");
                    break;  // This will trigger StopAsync() in the finally block
                }
                    
                await ProcessDelimitedMessagesAsync();
                    
                args.Completed -= OnReceiveCompleted;
            }
        }
        finally
        {
            args.Completed -= OnReceiveCompleted;
            _argsPool.Return(args);
        }
    }

    private void OnReceiveCompleted(object? sender, SocketAsyncEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e.UserToken, nameof(e.UserToken));
        
        var tcs = (TaskCompletionSource<int>)e.UserToken;
        if (e.SocketError == SocketError.Success)
        {
            tcs.TrySetResult(e.BytesTransferred);
        }
        else
        {
            tcs.TrySetException(new SocketException((int)e.SocketError));
        }
    }

    private async Task ProcessDelimitedMessagesAsync()
    {
        int delimiterPos;
        while ((delimiterPos = _stringBuffer.ToString().IndexOf(_delimiter)) != -1)
        {
            // Extract the message up to the delimiter
            string message = _stringBuffer.ToString(0, delimiterPos + 1);
                
            // Remove the processed message and delimiter from the buffer
            _stringBuffer.Remove(0, delimiterPos + 1);
                
            // Raise event with the message
            MessageReceived?.Invoke(this, message);
                
            // Allow event handler to complete
            await Task.Yield();
        }
    }

    public async Task SendAsync(string message)
    {
        if (!_isRunning) return;
            
        byte[] data = Encoding.UTF8.GetBytes(message);
        var args = _argsPool.Get();
        args.SetBuffer(data, 0, data.Length);
            
        try
        {
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                
            args.Completed += OnSendCompleted;
            args.UserToken = tcs;
                
            bool isPending = _socket.SendAsync(args);
            if (!isPending)
            {
                OnSendCompleted(_socket, args);
            }
                
            await tcs.Task;
        }
        finally
        {
            args.Completed -= OnSendCompleted;
            _argsPool.Return(args);
        }
    }

    private void OnSendCompleted(object? sender, SocketAsyncEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e.UserToken, nameof(e.UserToken));
        
        var tcs = (TaskCompletionSource<bool>)e.UserToken;
            
        if (e.SocketError == SocketError.Success)
        {
            tcs.TrySetResult(true);
        }
        else
        {
            tcs.TrySetException(new SocketException((int)e.SocketError));
        }
    }
}