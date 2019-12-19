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

        internal static IMySQLDataType[] DataTypes { get; private set; } = new IMySQLDataType[256];

        static LogEvent()
        {
            DataTypes[(int)ColumnType.BIT] = new BitType();
            DataTypes[(int)ColumnType.TINY] = new TinyType();
            DataTypes[(int)ColumnType.SHORT] = new ShortType();
            DataTypes[(int)ColumnType.INT24] = new Int24Type();
            DataTypes[(int)ColumnType.LONG] = new LongType();
            DataTypes[(int)ColumnType.LONGLONG] = new LongLongType();
            DataTypes[(int)ColumnType.FLOAT] = new FloatType();
            DataTypes[(int)ColumnType.DOUBLE] = new DoubleType();
            DataTypes[(int)ColumnType.NEWDECIMAL] = new NewDecimalType();
            DataTypes[(int)ColumnType.DATE] = new DateType();
            DataTypes[(int)ColumnType.STRING] = new StringType();
            DataTypes[(int)ColumnType.VARCHAR] = new VarCharType();
        }

        protected internal abstract void DecodeBody(ref SequenceReader<byte> reader, object context);

        public const int MARIA_SLAVE_CAPABILITY_GTID = 4;
        public const int MARIA_SLAVE_CAPABILITY_MINE = MARIA_SLAVE_CAPABILITY_GTID;

        private static readonly DateTime _unixEpoch = new DateTime(1970, 1, 1);

        internal static DateTime GetTimestapmFromUnixEpoch(int seconds)
        {
            return _unixEpoch.AddSeconds(seconds);
        }

        protected bool HasCRC { get; set; } = false;

        protected bool RebuildReaderAsCRC(ref SequenceReader<byte> reader)
        {
            if (!HasCRC || ChecksumType == ChecksumType.NONE)
                return false;

            reader = new SequenceReader<byte>(reader.Sequence.Slice(reader.Consumed, reader.Remaining - (int)ChecksumType));
            return true;
        }

        public override string ToString()
        {
            return EventType.ToString();
        }
    }
}
