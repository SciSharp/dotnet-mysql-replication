using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Buffers;
using System.Buffers.Binary;

namespace SciSharp.MySQL.Replication.Types
{
    /// <summary>
    /// Represents the MySQL BIGINT data type.
    /// </summary>
    /// <remarks>
    /// Handles the reading and conversion of MySQL BIGINT values (8-byte integers).
    /// </remarks>
    class LongLongType : IMySQLDataType
    {
        /// <summary>
        /// Reads a BIGINT value from the binary log.
        /// </summary>
        /// <param name="reader">The sequence reader containing the bytes to read.</param>
        /// <param name="columnMetadata">Metadata for the column.</param>
        /// <returns>A long value representing the MySQL BIGINT value.</returns>
        public object ReadValue(ref SequenceReader<byte> reader, ColumnMetadata columnMetadata)
        {
            Span<byte> buffer = stackalloc byte[sizeof(long)];

            reader.TryCopyTo(buffer);
            reader.Advance(sizeof(long));

            return columnMetadata.IsUnsigned
                ? BinaryPrimitives.ReadUInt64LittleEndian(buffer)
                : BinaryPrimitives.ReadInt64LittleEndian(buffer);
        }
    }
}
