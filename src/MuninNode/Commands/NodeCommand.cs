using System.Buffers;

namespace MuninNode.Commands;

public class NodeCommand(MuninNodeConfiguration config) : ICommand
{
    public ReadOnlySpan<byte> Name => "nodes"u8;

    public Task<HanldeResult> ProcessAsync(ReadOnlySequence<byte> args, CancellationToken cancellationToken)
    {
        var result = new HanldeResult
        {
            Lines = [
                config.Hostname,
                "."
            ],
            Status = Status.Continue
        };
        return Task.FromResult(result);

    }
}