namespace Program
{
    class Program
    {
        static async Task Main(string[] args)
        {
            string ipAddress = "127.0.0.1";
            int port = 8888;

            using var cts = new CancellationTokenSource();
            
            Console.CancelKeyPress += (s, e) => {
                e.Cancel = true;
                cts.Cancel();
                Console.WriteLine("Cancellation requested. Shutting down...");
            };

            await using var server = new AsyncSocket.AsyncTcpServer(ipAddress, port);
            var serverTask = server.RunAsync(cts.Token);

            Console.WriteLine("Server running. Press Ctrl+C to stop.");
            
            try
            {
                await serverTask;
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Server shutdown completed.");
            }
        }
    }
    
}