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
    class TimestampV2Type : TimeBaseType, IMySQLDataType
    {
        /// <summary>
        /// Reads a TIMESTAMP2 value from the binary log.
        /// </summary>
        /// <param name="reader">The sequence reader containing the bytes to read.</param>
        /// <param name="columnMetadata">Metadata for the column defining fractional second precision.</param>
        /// <returns>A DateTime object representing the MySQL TIMESTAMP2 value.</returns>
        public object ReadValue(ref SequenceReader<byte> reader, ColumnMetadata columnMetadata)
        {
            int meta = columnMetadata.MetadataValue;
            var millis = (long)reader.ReadBigEndianInteger(4);
            var fsp = ReadFractionalSeconds(ref reader, meta);
            var ticks = millis * 1000 + fsp / 1000;
            return new DateTime(ticks);
        }
    }
}
