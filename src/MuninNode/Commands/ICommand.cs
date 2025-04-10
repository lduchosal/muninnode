using System.Buffers;

namespace MuninNode.Commands;

public interface ICommand
{
    Task<HanldeResult> ProcessAsync(ReadOnlySequence<byte> args, CancellationToken cancellationToken);
    ReadOnlySpan<byte> Name { get; }
}