using System;
using System.Buffers;

namespace SciSharp.MySQL.Replication.Types
{
    /// <summary>
    /// Represents the MySQL YEAR data type.
    /// </summary>
    /// <remarks>
    /// Handles the reading and conversion of MySQL YEAR values.
    /// </remarks>
    class YearType : IMySQLDataType
    {
        /// <summary>
        /// Reads a YEAR value from the binary log.
        /// </summary>
        /// <param name="reader">The sequence reader containing the bytes to read.</param>
        /// <param name="columnMetadata">Metadata for the column.</param>
        /// <returns>An integer representing the MySQL YEAR value.</returns>
        public object ReadValue(ref SequenceReader<byte> reader, ColumnMetadata columnMetadata)
        {
            return 1900 + reader.ReadInteger(1);
        }
    }
}
