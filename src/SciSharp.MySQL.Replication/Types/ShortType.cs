using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Buffers;
using System.Buffers.Binary;

namespace SciSharp.MySQL.Replication.Types
{
    /// <summary>
    /// Represents the MySQL SMALLINT data type.
    /// </summary>
    /// <remarks>
    /// Handles the reading and conversion of MySQL SMALLINT values (2-byte integers).
    /// </remarks>
    class ShortType : IMySQLDataType
    {
        /// <summary>
        /// Reads a SMALLINT value from the binary log.
        /// </summary>
        /// <param name="reader">The sequence reader containing the bytes to read.</param>
        /// <param name="columnMetadata">Metadata for the column.</param>
        /// <returns>A short value representing the MySQL SMALLINT value.</returns>
        public object ReadValue(ref SequenceReader<byte> reader, ColumnMetadata columnMetadata)
        {
            Span<byte> buffer = stackalloc byte[sizeof(short)];

            reader.TryCopyTo(buffer);
            reader.Advance(sizeof(short));

            return columnMetadata.IsUnsigned
                ? (ushort)BinaryPrimitives.ReadUInt16LittleEndian(buffer)
                : (short)BinaryPrimitives.ReadInt16LittleEndian(buffer);
        }
    }
}
