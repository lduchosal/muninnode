using System.Buffers;

namespace MuninNode.Commands;

public class NodeCommand(MuninNodeConfiguration config) : ICommand
{
    public Task<string[]> ProcessAsync(ReadOnlySequence<byte> args, CancellationToken cancellationToken)
    {
        return Task.FromResult<string[]>(
        [
            config.Hostname,
            "."
        ]);
    }
}