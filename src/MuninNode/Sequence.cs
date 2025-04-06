using System.Buffers;

namespace MuninNode;

public static class Sequence
{
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

}