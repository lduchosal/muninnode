using System.Buffers;
using MuninNode.Server;

namespace MuninNode.Commands;

public class CapCommand : ICommand
{
    public ReadOnlySpan<byte> Name => "cap"u8;

    public Task<HanldeResult> ProcessAsync(ReadOnlySequence<byte> args, CancellationToken cancellationToken)
    {
        // TODO: multigraph (https://guide.munin-monitoring.org/en/latest/plugin/protocol-multigraph.html)
        // TODO: dirtyconfig (https://guide.munin-monitoring.org/en/latest/plugin/protocol-dirtyconfig.html)
        // XXX: ignores capability arguments
        var result2 = new HanldeResult
        {
            Lines = ["cap"],
            Status = Status.Continue
        };
        return Task.FromResult(result2);
    }
}