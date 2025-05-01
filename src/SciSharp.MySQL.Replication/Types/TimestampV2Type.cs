using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Buffers;

namespace SciSharp.MySQL.Replication.Types
{
    /// <summary>
    /// Represents the MySQL TIMESTAMP2 data type.
    /// </summary>
    /// <remarks>
    /// Handles the reading and conversion of MySQL TIMESTAMP values with fractional seconds.
    /// </remarks>
    class TimestampV2Type : TimeBaseType, IMySQLDataType, IColumnMetadataLoader
    {
        // Unix epoch start for MySQL TIMESTAMP (1970-01-01 00:00:00 UTC)
        private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>
        /// Loads metadata for TIMESTAMP2 type.
        /// </summary>
        /// <param name="columnMetadata">The column metadata object.</param>
        public void LoadMetadataValue(ColumnMetadata columnMetadata)
        {
            // For TIMESTAMP2, the metadata value represents the fractional seconds precision (0-6)
            columnMetadata.Options = new TimestampV2Options
            {
                FractionalSecondsPrecision = columnMetadata.MetadataValue[0]
            };
        }

        /// <summary>
        /// Reads a TIMESTAMP2 value from the binary log.
        /// </summary>
        /// <param name="reader">The sequence reader containing the bytes to read.</param>
        /// <param name="columnMetadata">Metadata for the column defining fractional second precision.</param>
        /// <returns>A DateTime object representing the MySQL TIMESTAMP2 value.</returns>
        public object ReadValue(ref SequenceReader<byte> reader, ColumnMetadata columnMetadata)
        {
            // Get the precision from metadata
            int fsp = columnMetadata.Options is TimestampV2Options options ? 
                options.FractionalSecondsPrecision : 0;

            // Read the 4-byte seconds part (big-endian integer representing seconds since Unix epoch)
            byte b0, b1, b2, b3;
            reader.TryRead(out b0);
            reader.TryRead(out b1);
            reader.TryRead(out b2);
            reader.TryRead(out b3);
            
            uint secondsSinceEpoch = ((uint)b0 << 24) | ((uint)b1 << 16) | ((uint)b2 << 8) | b3;
            
            // Start with the base DateTime (seconds part)
            DateTime timestamp = UnixEpoch.AddSeconds(secondsSinceEpoch);

            // Read fractional seconds if precision > 0
            if (fsp > 0)
            {
                int fractionalValue = ReadFractionalSeconds(ref reader, fsp);
                
                // Calculate microseconds
                int microseconds = 0;
                switch (fsp)
                {
                    case 1: microseconds = fractionalValue * 100000; break;
                    case 2: microseconds = fractionalValue * 10000; break;
                    case 3: microseconds = fractionalValue * 1000; break;
                    case 4: microseconds = fractionalValue * 100; break;
                    case 5: microseconds = fractionalValue * 10; break;
                    case 6: microseconds = fractionalValue; break;
                }
                
                // Add microsecond precision to the DateTime
                // 1 tick = 100 nanoseconds, 1 microsecond = 10 ticks
                timestamp = timestamp.AddTicks(microseconds * 10);
            }
            
            return timestamp;
        }
    }

    /// <summary>
    /// Options specific to TIMESTAMP2 data type.
    /// </summary>
    class TimestampV2Options
    {
        /// <summary>
        /// Gets or sets the precision of fractional seconds (0-6).
        /// </summary>
        public int FractionalSecondsPrecision { get; set; }
    }
}
