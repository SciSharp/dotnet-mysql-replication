using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Buffers;

namespace SciSharp.MySQL.Replication.Types
{
    /// <summary>
    /// Represents the MySQL TIMESTAMP data type.
    /// </summary>
    /// <remarks>
    /// Handles the reading and conversion of MySQL TIMESTAMP values.
    /// </remarks>
    class TimestampType : IMySQLDataType
    {
        // Unix epoch start for MySQL TIMESTAMP (1970-01-01 00:00:00 UTC)
        private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>
        /// Reads a TIMESTAMP value from the binary log.
        /// </summary>
        /// <param name="reader">The sequence reader containing the bytes to read.</param>
        /// <param name="columnMetadata">Metadata for the column.</param>
        /// <returns>A DateTime object representing the MySQL TIMESTAMP value.</returns>
        public object ReadValue(ref SequenceReader<byte> reader, ColumnMetadata columnMetadata)
        {
            // MySQL TIMESTAMP is stored as a 4-byte integer 
            // representing seconds since Unix epoch (1970-01-01 00:00:00 UTC)
            
            // Read the 4 bytes as a 32-bit unsigned integer
            uint secondsSinceEpoch = 0;
            
            // Read each byte individually
            reader.TryRead(out byte b0);
            reader.TryRead(out byte b1);
            reader.TryRead(out byte b2);
            reader.TryRead(out byte b3);
            
            // Combine bytes to form the 32-bit unsigned int (big-endian)
            secondsSinceEpoch = ((uint)b0 << 24) | 
                                ((uint)b1 << 16) | 
                                ((uint)b2 << 8) | 
                                b3;
            
            // Convert Unix timestamp to DateTime
            // MySQL stores TIMESTAMP in UTC, so we return it as UTC DateTime
            DateTime timestamp = UnixEpoch.AddSeconds(secondsSinceEpoch);
            
            return timestamp;
        }
    }
}
