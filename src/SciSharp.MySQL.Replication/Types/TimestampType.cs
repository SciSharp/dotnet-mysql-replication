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
        /// <summary>
        /// Reads a TIMESTAMP value from the binary log.
        /// </summary>
        /// <param name="reader">The sequence reader containing the bytes to read.</param>
        /// <param name="meta">Metadata for the column.</param>
        /// <returns>A DateTime object representing the MySQL TIMESTAMP value.</returns>
        public object ReadValue(ref SequenceReader<byte> reader, int meta)
        {
            return new DateTime(reader.ReadLong(4) * 1000);
        }
    }
}
