using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Buffers;

namespace SciSharp.MySQL.Replication
{
    /// <summary>
    /// Represents the MySQL DOUBLE data type.
    /// </summary>
    /// <remarks>
    /// Handles the reading and conversion of MySQL DOUBLE precision floating-point values.
    /// </remarks>
    class DoubleType : IMySQLDataType
    {
        /// <summary>
        /// Reads a DOUBLE value from the binary log.
        /// </summary>
        /// <param name="reader">The sequence reader containing the bytes to read.</param>
        /// <param name="meta">Metadata for the column.</param>
        /// <returns>A double value representing the MySQL DOUBLE value.</returns>
        public object ReadValue(ref SequenceReader<byte> reader, int meta)
        {
            return BitConverter.Int64BitsToDouble(reader.ReadLong(8));
        }
    }
}
