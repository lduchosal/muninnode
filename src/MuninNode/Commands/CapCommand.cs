using System.Buffers;

namespace MuninNode.Commands;

public class CapCommand : ICommand
{
    public Task<string[]> ProcessAsync(ReadOnlySequence<byte> args, CancellationToken cancellationToken)
    {
        // TODO: multigraph (https://guide.munin-monitoring.org/en/latest/plugin/protocol-multigraph.html)
        // TODO: dirtyconfig (https://guide.munin-monitoring.org/en/latest/plugin/protocol-dirtyconfig.html)
        // XXX: ignores capability arguments
        var result = "cap";
        return Task.FromResult<string[]>([ result ]);
    }
}