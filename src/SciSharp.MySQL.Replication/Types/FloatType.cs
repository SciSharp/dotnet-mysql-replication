using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Buffers;

namespace SciSharp.MySQL.Replication.Types
{
    /// <summary>
    /// Represents the MySQL FLOAT data type.
    /// </summary>
    /// <remarks>
    /// Handles the reading and conversion of MySQL FLOAT values.
    /// </remarks>
    class FloatType : IMySQLDataType
    {
        /// <summary>
        /// Reads a FLOAT value from the binary log.
        /// </summary>
        /// <param name="reader">The sequence reader containing the bytes to read.</param>
        /// <param name="columnMetadata">Metadata for the column.</param>
        /// <returns>A float value representing the MySQL FLOAT value.</returns>
        public object ReadValue(ref SequenceReader<byte> reader, ColumnMetadata columnMetadata)
        {
            byte[] bytes = BitConverter.GetBytes(reader.ReadInteger(4));
            return BitConverter.ToSingle(bytes, 0);
        }
    }
}
