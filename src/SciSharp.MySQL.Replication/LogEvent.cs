using System;
using System.Buffers;

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
        protected internal abstract void DecodeBody(ref SequenceReader<byte> reader);

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
