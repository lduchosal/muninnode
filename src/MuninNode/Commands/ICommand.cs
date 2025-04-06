using System.Buffers;
using System.Threading.Tasks;

namespace MuninNode.Commands;

public interface ICommand
{
    Task<string[]> ProcessAsync(ReadOnlySequence<byte> args, CancellationToken cancellationToken);
    ReadOnlySpan<byte> Name { get; }
}