namespace MuninNode;

public interface IMuninNode {
  Task RunAsync(CancellationToken stoppingToken);
}
