using System.Threading;
using System.Threading.Tasks;

namespace MuninNode;

public interface IMuninNode {
  Task RunAsync(CancellationToken stoppingToken);
}
