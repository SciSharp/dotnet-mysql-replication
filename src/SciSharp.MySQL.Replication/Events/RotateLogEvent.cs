using System;
using System.Buffers;
using System.Text;
using SuperSocket.ProtoBase;

namespace SciSharp.MySQL.Replication
{
    /// <summary>
    /// https://dev.mysql.com/doc/internals/en/rotate-event.html
    /// </summary>
    public sealed class RotateLogEvent : LogEvent
    {
        public long RotatePosition { get; set; }

        public string NextBinlogFileName { get; set; }

        protected internal override void DecodeBody(ref SequenceReader<byte> reader)
        {
            reader.TryReadLittleEndian(out long position);
            RotatePosition = position;

            reader.TryReadTo(out ReadOnlySequence<byte> sequence, 0x00, false);
            NextBinlogFileName = sequence.GetString(Encoding.UTF8);

            reader.Advance(1);
        }
    }
}
