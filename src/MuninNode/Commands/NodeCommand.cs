using System.Buffers;

namespace MuninNode.Commands;

public class NodeCommand(MuninNodeConfiguration config) : ICommand
{
    public ReadOnlySpan<byte> Name => "nodes"u8;

    public Task<string[]> ProcessAsync(ReadOnlySequence<byte> args, CancellationToken cancellationToken)
    {
        return Task.FromResult<string[]>(
        [
            config.Hostname,
            "."
        ]);
    }
}