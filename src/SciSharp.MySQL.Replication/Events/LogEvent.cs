using System;
using System.Buffers;
using SciSharp.MySQL.Replication.Types;

namespace SciSharp.MySQL.Replication.Events
{
    /// <summary>
    /// Base class for all MySQL binary log events.
    /// </summary>
    public abstract class LogEvent
    {
        /// <summary>
        /// Gets or sets the checksum type used for log events.
        /// </summary>
        public static ChecksumType ChecksumType { get; internal set; }
        
        /// <summary>
        /// Gets or sets the timestamp when the event was created.
        /// </summary>
        public DateTime Timestamp { get; set; }
        
        /// <summary>
        /// Gets or sets the type of the log event.
        /// </summary>
        public LogEventType EventType { get; internal set; }
        
        /// <summary>
        /// Gets or sets the server ID that generated the event.
        /// </summary>
        public int ServerID { get; set; }
        
        /// <summary>
        /// Gets or sets the total size of the event in bytes.
        /// </summary>
        public int EventSize { get; set; }
        
        /// <summary>
        /// Gets or sets the position of the event in the binary log.
        /// </summary>
        public int Position { get; set; }
        
        /// <summary>
        /// Gets or sets the flags associated with the event.
        /// </summary>
        public LogEventFlag Flags { get; set; }

        /// <summary>
        /// Gets or sets the array of MySQL data type handlers.
        /// </summary>
        internal static IMySQLDataType[] DataTypes { get; private set; } = new IMySQLDataType[256];

        /// <summary>
        /// Static constructor to initialize data type handlers.
        /// </summary>
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
            DataTypes[(int)ColumnType.DATETIME] = new DateTimeType();
            DataTypes[(int)ColumnType.DATETIME_V2] = new DateTimeV2Type();
            DataTypes[(int)ColumnType.TIME] = new TimeType();
            DataTypes[(int)ColumnType.TIME_V2] = new TimeV2Type();
            DataTypes[(int)ColumnType.TIMESTAMP] = new TimestampType();
            DataTypes[(int)ColumnType.TIMESTAMP_V2] = new TimestampV2Type();
            DataTypes[(int)ColumnType.ENUM] = new EnumType();
            DataTypes[(int)ColumnType.SET] = new SetType();
            DataTypes[(int)ColumnType.BLOB] = new BlobType();
        }

        /// <summary>
        /// Decodes the body of the log event from the binary data.
        /// </summary>
        /// <param name="reader">The sequence reader containing binary data.</param>
        /// <param name="context">The context object containing additional information.</param>
        protected internal abstract void DecodeBody(ref SequenceReader<byte> reader, object context);

        /// <summary>
        /// Maria DB slave capability flag for GTID support.
        /// </summary>
        public const int MARIA_SLAVE_CAPABILITY_GTID = 4;
        
        /// <summary>
        /// Maria DB slave capability flags used by this implementation.
        /// </summary>
        public const int MARIA_SLAVE_CAPABILITY_MINE = MARIA_SLAVE_CAPABILITY_GTID;

        /// <summary>
        /// The Unix epoch reference time (1970-01-01).
        /// </summary>
        private static readonly DateTime _unixEpoch = new DateTime(1970, 1, 1);

        /// <summary>
        /// Converts Unix timestamp (seconds since epoch) to DateTime.
        /// </summary>
        /// <param name="seconds">Seconds since Unix epoch.</param>
        /// <returns>Corresponding DateTime.</returns>
        internal static DateTime GetTimestampFromUnixEpoch(int seconds)
        {
            return _unixEpoch.AddSeconds(seconds);
        }

        /// <summary>
        /// Gets or sets a value indicating whether this event has a CRC checksum.
        /// </summary>
        protected bool HasCRC { get; set; } = false;

        /// <summary>
        /// Rebuilds the reader to exclude the CRC checksum from the data if needed.
        /// </summary>
        /// <param name="reader">The sequence reader to modify.</param>
        /// <returns>True if the reader was modified, otherwise false.</returns>
        protected bool RebuildReaderAsCRC(ref SequenceReader<byte> reader)
        {
            if (!HasCRC || ChecksumType == ChecksumType.NONE)
                return false;

            reader = new SequenceReader<byte>(reader.Sequence.Slice(reader.Consumed, reader.Remaining - (int)ChecksumType));
            return true;
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString()
        {
            return EventType.ToString();
        }
    }
}
