using System.Buffers;

namespace MuninNode.Commands;

public class VersionCommand(MuninNodeConfiguration config) : ICommand
{
    public ReadOnlySpan<byte> Name => "version"u8;
    private static readonly Version DefaultNodeVersion = new(1, 0, 0, 0);
    private Version NodeVersion => DefaultNodeVersion;

    public Task<string[]> ProcessAsync(ReadOnlySequence<byte> args, CancellationToken cancellationToken)
    {
        var result = $"munins node on {config.Hostname} version: {NodeVersion}";
        return Task.FromResult<string[]>([result]);
    }
}