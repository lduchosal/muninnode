using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace Program.AsyncSocket;

class AsyncTcpServer : IAsyncDisposable
{
    private readonly IPEndPoint _endpoint;
    private readonly Socket _listener;
    private readonly ConcurrentDictionary<Guid, ClientSession> _clients = new();
    private readonly SemaphoreSlim _maxConnectionsSemaphore;
    private readonly SocketAsyncEventArgsPool _argsPool = new();
    
    private const int BufferSize = 4096;
    private const int MaxConnections = 100;
    
    public AsyncTcpServer(string ipAddress, int port)
    {
        _endpoint = new IPEndPoint(IPAddress.Parse(ipAddress), port);
        _maxConnectionsSemaphore = new SemaphoreSlim(MaxConnections, MaxConnections);
        _listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        _listener.Bind(_endpoint);
        _listener.Listen(100);

        Console.WriteLine($"Server started. Listening on {_endpoint}");

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await _maxConnectionsSemaphore.WaitAsync(cancellationToken);

                var tcs = new TaskCompletionSource<Socket>(TaskCreationOptions.RunContinuationsAsynchronously);
                var acceptArgs = new SocketAsyncEventArgs();
                acceptArgs.UserToken = tcs;

                acceptArgs.Completed += OnAcceptArgsOnCompleted;

                bool isPending = _listener.AcceptAsync(acceptArgs);
                if (!isPending)
                {
                    OnAcceptArgsOnCompleted(this, acceptArgs);
                }

                // Handle new client in a separate task
                _ = AcceptClientAsync(tcs.Task, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation is requested
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in server: {ex.Message}");
        }
    }
    
    void OnAcceptArgsOnCompleted(object? s, SocketAsyncEventArgs e)
    {
        var tcs = (TaskCompletionSource<Socket>)e.UserToken!;
        if (e.SocketError == SocketError.Success
            && e.AcceptSocket != null)
        {
            tcs.TrySetResult(e.AcceptSocket);
        }
        else
        {
            tcs.TrySetException(new SocketException((int)e.SocketError));
        }
        e.Dispose();
    }

    private async Task AcceptClientAsync(Task<Socket> acceptTask, CancellationToken cancellationToken)
    {
        try
        {
            var clientSocket = await acceptTask;
            var clientId = Guid.NewGuid();
            var client = new ClientSession(clientId, clientSocket, '\n', BufferSize, _argsPool);

            await HandleConnectedAsync(client);

            _clients.TryAdd(clientId, client);
            client.MessageReceived += async (sender, message) =>
            {
                await HandleMessageAsync(client, message);
            };
                
            client.Disconnected += async (sender, id) =>
            {
                await HandleDisconnectedAsync(client);

                _clients.TryRemove(id, out _);
                _maxConnectionsSemaphore.Release();
            };
                
            Console.WriteLine($"Client connected: {clientSocket.RemoteEndPoint} (ID: {clientId})");
                
            await client.StartAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error accepting client: {ex.Message}");
            _maxConnectionsSemaphore.Release();
        }
    }

    private Task HandleConnectedAsync(ClientSession client)
    {
        Console.WriteLine($"Client Connected {client.Id}");
        return Task.CompletedTask;
    }
    private Task HandleDisconnectedAsync(ClientSession client)
    {
        Console.WriteLine($"Client Disconnected {client.Id}");
        return Task.CompletedTask;
    }
    
    private async Task HandleMessageAsync(ClientSession client, string message)
    {
        Console.WriteLine($"Received from {client.Id}: {message}");
            
        // Echo the message back with the delimiter
        string response = $"Server received: {message}";
        await client.SendAsync(response);
    }

    public async ValueTask DisposeAsync()
    {
        _listener?.Close();

        var clientTasks = new List<Task>();
        foreach (var client in _clients.Values)
        {
            clientTasks.Add(client.StopAsync());
        }
        await Task.WhenAll(clientTasks);
        _clients.Clear();

        _maxConnectionsSemaphore.Dispose();
        _argsPool.Dispose();

        Console.WriteLine("Server stopped.");
    }
}