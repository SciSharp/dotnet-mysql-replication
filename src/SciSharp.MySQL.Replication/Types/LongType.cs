using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Buffers;
using System.Buffers.Binary;

namespace SciSharp.MySQL.Replication.Types
{
    /// <summary>
    /// Represents the MySQL INT data type.
    /// </summary>
    /// <remarks>
    /// Handles the reading and conversion of MySQL INT values (4-byte integers).
    /// </remarks>
    class LongType : IMySQLDataType
    {
        /// <summary>
        /// Reads an INT value from the binary log.
        /// </summary>
        /// <param name="reader">The sequence reader containing the bytes to read.</param>
        /// <param name="columnMetadata">Metadata for the column.</param>
        /// <returns>An integer representing the MySQL INT value.</returns>
        public object ReadValue(ref SequenceReader<byte> reader, ColumnMetadata columnMetadata)
        {
            Span<byte> buffer = stackalloc byte[sizeof(int)];

            reader.TryCopyTo(buffer);
            reader.Advance(sizeof(int));

            return columnMetadata.IsUnsigned
                ? BinaryPrimitives.ReadUInt32LittleEndian(buffer)
                : BinaryPrimitives.ReadInt32LittleEndian(buffer);
        }
    }
}
