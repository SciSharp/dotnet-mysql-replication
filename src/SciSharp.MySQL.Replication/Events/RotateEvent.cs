using System;
using System.Buffers;
using System.Text;
using SuperSocket.ProtoBase;

namespace SciSharp.MySQL.Replication
{
    /// <summary>
    /// https://dev.mysql.com/doc/internals/en/rotate-event.html
    /// </summary>
    public sealed class RotateEvent : LogEvent
    {
        public long RotatePosition { get; set; }

        public string NextBinlogFileName { get; set; }

        protected internal override void DecodeBody(ref SequenceReader<byte> reader)
        {
            reader.TryReadLittleEndian(out long position);
            RotatePosition = position;

            var binglogFileNameSize = reader.Remaining - (int)LogEvent.ChecksumType;

            NextBinlogFileName = reader.Sequence.Slice(reader.Consumed, binglogFileNameSize).GetString(Encoding.UTF8);
            reader.Advance(binglogFileNameSize);
        }
    }
}
