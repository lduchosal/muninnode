using System.Buffers;

namespace MuninNode;

public static class Sequence
{
    public static readonly ReadOnlyMemory<byte> EndOfLine = new[] { (byte)'\n' };

    public static bool TryReadLine(ref ReadOnlySequence<byte> buffer, out ReadOnlySequence<byte> line)
    {
        var reader = new SequenceReader<byte>(buffer);
        const byte LF = (byte)'\n';
        var CRLF = "\r\n"u8;

        if (
            !reader.TryReadTo(out line, delimiter: CRLF, advancePastDelimiter: true) &&
            !reader.TryReadTo(out line, delimiter: LF, advancePastDelimiter: true)
        )
        {
            line = default;
            return false;
        }

#if SYSTEM_BUFFERS_SEQUENCEREADER_UNREADSEQUENCE
            buffer = reader.UnreadSequence;
#else
        buffer = reader.Sequence.Slice(reader.Position);
#endif

        return true;
    }

    public static bool Expect(
        this ReadOnlySequence<byte> commandLine,
        ReadOnlySpan<byte> expectedCommand,
        out ReadOnlySequence<byte> arguments
    )
    {
        arguments = default;

        var reader = new SequenceReader<byte>(commandLine);

        if (!reader.IsNext(expectedCommand, advancePast: true))
        {
            return false;
        }

        const byte space = (byte)' ';

        if (reader.Remaining == 0)
        {
            // <command> <EOL>
            arguments = default;
            return true;
        }
        else if (reader.IsNext(space, advancePast: true))
        {
            // <command> <SP> <arguments> <EOL>
#if SYSTEM_BUFFERS_SEQUENCEREADER_UNREADSEQUENCE
            arguments = reader.UnreadSequence;
#else
            arguments = reader.Sequence.Slice(reader.Position);
#endif
            return true;
        }

        return false;
    }
}