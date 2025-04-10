using System.Buffers;

namespace MuninNode.Commands;

public class ShortQuitCommand : QuitCommand
{
    public new ReadOnlySpan<byte> Name => "."u8;
}
