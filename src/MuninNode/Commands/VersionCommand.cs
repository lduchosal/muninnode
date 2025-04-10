using System.Buffers;

namespace MuninNode.Commands;

public class VersionCommand(MuninNodeConfiguration config) : ICommand
{
    public ReadOnlySpan<byte> Name => "version"u8;
    private static readonly Version DefaultNodeVersion = new(1, 0, 0, 0);
    private static Version NodeVersion => DefaultNodeVersion;

    public Task<HanldeResult> ProcessAsync(ReadOnlySequence<byte> args, CancellationToken cancellationToken)
    {
        var result = new HanldeResult
        {
            Lines = [$"munins node on {config.Hostname} version: {NodeVersion}"],
            Status = Status.Continue
        };

        return Task.FromResult(result);
    }
}