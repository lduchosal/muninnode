using System.Buffers;

namespace MuninNode.Commands;

public class HelpCommand : ICommand, IDefaultCommand
{
    public ReadOnlySpan<byte> Name => "help"u8;

    public Task<string[]> ProcessAsync(ReadOnlySequence<byte> args, CancellationToken cancellationToken)
    {
        const string result = "# Unknown command. Try help, cap, list, nodes, config, fetch, version or quit";
        return Task.FromResult<string[]>([result]);
    }
}