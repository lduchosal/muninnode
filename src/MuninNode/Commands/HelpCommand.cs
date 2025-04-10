using System.Buffers;

namespace MuninNode.Commands;

public class HelpCommand : ICommand, IDefaultCommand
{
    public ReadOnlySpan<byte> Name => "help"u8;

    public Task<HanldeResult> ProcessAsync(ReadOnlySequence<byte> args, CancellationToken cancellationToken)
    {
        const string help = "# Unknown command. Try help, cap, list, nodes, config, fetch, version or quit";
        var result = new HanldeResult
        {
            Lines = [ help ],
            Status = Status.Continue
        };
        return Task.FromResult(result);
    }
}