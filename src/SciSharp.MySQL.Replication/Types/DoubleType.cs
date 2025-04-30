using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Buffers;

namespace SciSharp.MySQL.Replication.Types
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
        /// <param name="columnMetadata">Metadata for the column.</param>
        /// <returns>A double value representing the MySQL DOUBLE value.</returns>
        public object ReadValue(ref SequenceReader<byte> reader, ColumnMetadata columnMetadata)
        {
            // MySQL stores DOUBLE values in IEEE 754 double-precision format (8 bytes)
            // Read the 8 bytes in big-endian order
            Span<byte> buffer = stackalloc byte[8];
            reader.TryCopyTo(buffer);
            reader.Advance(8);
            
            return BitConverter.ToDouble(buffer);
        }
    }
}
