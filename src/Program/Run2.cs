namespace Program;

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

class Program2
{
    static async Task Main(string[] args)
    {
        // Server configuration
        string ipAddress = "127.0.0.1";
        int port = 8888;

        // Create and start the server
        var server = new DelimitedTcpServer(ipAddress, port);
        await server.StartAsync();

        Console.WriteLine("Press Enter to stop the server...");
        Console.ReadLine();
        
        await server.StopAsync();
    }
}

class DelimitedTcpServer
{
    private readonly TcpListener _listener;
    private readonly List<TcpClient> _clients = new List<TcpClient>();
    private bool _isRunning;
    private readonly char _delimiter = '\n';

    public DelimitedTcpServer(string ipAddress, int port)
    {
        IPAddress localAddr = IPAddress.Parse(ipAddress);
        _listener = new TcpListener(localAddr, port);
    }

    public async Task StartAsync()
    {
        _listener.Start();
        _isRunning = true;
        
        Console.WriteLine($"Server started. Listening on {((IPEndPoint)_listener.LocalEndpoint).Address}:{((IPEndPoint)_listener.LocalEndpoint).Port}");
        
        try
        {
            while (_isRunning)
            {
                var client = await _listener.AcceptTcpClientAsync();
                _clients.Add(client);
                
                // Handle each client in its own task
                _ = HandleClientAsync(client);
            }
        }
        catch (Exception ex) when (_isRunning)
        {
            Console.WriteLine($"Error in server: {ex.Message}");
        }
    }

    public async Task StopAsync()
    {
        _isRunning = false;
        
        // Close all client connections
        foreach (var client in _clients.ToArray())
        {
            client.Close();
        }
        _clients.Clear();
        
        // Stop listening
        _listener.Stop();
        
        await Task.CompletedTask;
        Console.WriteLine("Server stopped.");
    }

    private async Task HandleClientAsync(TcpClient client)
    {
        Console.WriteLine($"Client connected: {client.Client.RemoteEndPoint}");
        
        try
        {
            using (client)
            {
                var stream = client.GetStream();
                var buffer = new byte[4096];
                var data = new StringBuilder();
                
                while (_isRunning && client.Connected)
                {
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0)
                    {
                        // Client disconnected
                        break;
                    }
                    
                    // Convert received data to string and add to buffer
                    string receivedText = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    data.Append(receivedText);
                    
                    // Process any complete messages (delimited by \n)
                    ProcessDelimitedMessages(data, stream);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling client: {ex.Message}");
        }
        finally
        {
            Console.WriteLine($"Client disconnected: {client.Client.RemoteEndPoint}");
            _clients.Remove(client);
        }
    }

    private async void ProcessDelimitedMessages(StringBuilder buffer, NetworkStream stream)
    {
        int delimiterPos;
        while ((delimiterPos = buffer.ToString().IndexOf(_delimiter)) != -1)
        {
            // Extract the message up to the delimiter
            string message = buffer.ToString(0, delimiterPos);
            
            // Remove the processed message and delimiter from the buffer
            buffer.Remove(0, delimiterPos + 1);
            
            // Process the message
            await ProcessMessageAsync(message, stream);
        }
    }

    private async Task ProcessMessageAsync(string message, NetworkStream stream)
    {
        Console.WriteLine($"Received message: {message}");
        
        // Echo the message back with the delimiter
        string response = $"Server received: {message}{_delimiter}";
        byte[] responseBytes = Encoding.UTF8.GetBytes(response);
        await stream.WriteAsync(responseBytes, 0, responseBytes.Length);
    }
}
