using System;
using System.Buffers;
using System.Collections;
using System.Text;
using SuperSocket.ProtoBase;

namespace SciSharp.MySQL.Replication
{
    public abstract class LogEvent
    {
        public static ChecksumType ChecksumType { get; internal set; }        
        public DateTime Timestamp { get; set; }
        public LogEventType EventType { get; internal set; }
        public int ServerID { get; set; }
        public int EventSize { get; set; }
        public int Position { get; set; }
        public LogEventFlag Flags { get; set; }
        protected internal abstract void DecodeBody(ref SequenceReader<byte> reader, object context);

        protected BitArray ReadBitmap(ref SequenceReader<byte> reader, int length)
        {
            var dataLen = (length + 7) / 8;
            var array = new BitArray(length, false);
            var j = 0;

            for (var i = 0; i < dataLen; i++)
            {
                reader.TryRead(out byte b);
                
                if ((b & (0x01 << (j % 8))) != 0x00)
                    array[j] = true;
            }

            return array;
        }

        protected string ReadString(ref SequenceReader<byte> reader)
        {
            return ReadString(ref reader, out long length);
        }

        protected string ReadString(ref SequenceReader<byte> reader, out long length)
        {
            if (reader.TryReadTo(out ReadOnlySequence<byte> seq, 0x00, false))
            {
                length = seq.Length + 1;
                var result = seq.GetString(Encoding.UTF8);
                reader.Advance(1);
                return result;
            }
            else
            {
                length = reader.Remaining;
                seq = reader.Sequence;
                seq = seq.Slice(reader.Consumed);
                var result = seq.GetString(Encoding.UTF8);
                reader.Advance(length);
                return result;
            }
        }
        
        protected string ReadString(ref SequenceReader<byte> reader, int length = 0)
        {
            if (length == 0 || reader.Remaining <= length)
                return ReadString(ref reader);

            // reader.Remaining > length
            var seq = reader.Sequence.Slice(reader.Consumed, length);            
            var consumed = 0L;
            
            try
            {
                var subReader = new SequenceReader<byte>(seq);
                return ReadString(ref subReader, out consumed);
            }
            finally
            {
                reader.Advance(consumed);
            }
        }

        protected long ReadLong(ref SequenceReader<byte> reader, int length)
        {
            var unit = 1;
            var value = 0L;

            for (var i = 0; i < length; i++)
            {
                reader.TryRead(out byte thisValue);
                value += thisValue * unit;
                unit *= 256;
            }

            return value;
        }
        protected long ReadLengthEncodedInteger(ref SequenceReader<byte> reader)
        {
            reader.TryRead(out byte b0);            

            if (b0 == 0xFC)
            {
                reader.TryReadLittleEndian(out short shortValue);
                return (long)shortValue;
            }

            if (b0 == 0xFD)
            {
                reader.TryRead(out byte b1);
                reader.TryRead(out byte b2);
                reader.TryRead(out byte b3);

                return (long)(b1 + b2 * 256 + b3 * 256 * 256);
            }

            if (b0 == 0xFE)
            {
                reader.TryReadLittleEndian(out long longValue);
                return longValue;
            }

            return (long)b0;
        }

        public const int MARIA_SLAVE_CAPABILITY_GTID = 4;
        public const int MARIA_SLAVE_CAPABILITY_MINE = MARIA_SLAVE_CAPABILITY_GTID;

        private static readonly DateTime _unixEpoch = new DateTime(1970, 1, 1);

        internal static DateTime GetTimestapmFromUnixEpoch(int seconds)
        {
            return _unixEpoch.AddSeconds(seconds);
        }
    }
}
