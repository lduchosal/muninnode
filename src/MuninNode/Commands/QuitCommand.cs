using System.Buffers;

namespace MuninNode.Commands;

public class QuitCommand : ICommand
{
    public ReadOnlySpan<byte> Name => "quit"u8;

    public Task<HanldeResult> ProcessAsync(ReadOnlySequence<byte> args, CancellationToken cancellationToken)
    {
        var result = new HanldeResult
        {
            Lines = [],
            Status = Status.Quit
        };
        return Task.FromResult<HanldeResult>(result);
    }
}
