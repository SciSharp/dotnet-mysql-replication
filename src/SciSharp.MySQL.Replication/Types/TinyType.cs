using System;
using System.Buffers;

namespace SciSharp.MySQL.Replication.Types
{
    /// <summary>
    /// Represents the MySQL TINYINT data type.
    /// </summary>
    /// <remarks>
    /// Handles the reading and conversion of MySQL TINYINT values (1-byte integers).
    /// </remarks>
    class TinyType : IMySQLDataType
    {
        /// <summary>
        /// Reads a TINYINT value from the binary log.
        /// </summary>
        /// <param name="reader">The sequence reader containing the bytes to read.</param>
        /// <param name="columnMetadata">Metadata for the column.</param>
        /// <returns>A byte value representing the MySQL TINYINT value.</returns>
        public object ReadValue(ref SequenceReader<byte> reader, ColumnMetadata columnMetadata)
        {
            reader.TryRead(out byte x);
            return columnMetadata.IsUnsigned
                ? x
                : (sbyte)x;
        }
    }
}
