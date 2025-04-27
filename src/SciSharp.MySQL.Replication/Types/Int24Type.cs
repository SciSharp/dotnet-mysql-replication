using System;
using System.Buffers;
using System.Buffers.Binary;

namespace SciSharp.MySQL.Replication.Types
{
    /// <summary>
    /// Represents the MySQL MEDIUMINT data type (3-byte integer).
    /// </summary>
    /// <remarks>
    /// Handles the reading and conversion of MySQL MEDIUMINT values.
    /// </remarks>
    class Int24Type : IMySQLDataType
    {
        /// <summary>
        /// Reads a MEDIUMINT value from the binary log.
        /// </summary>
        /// <param name="reader">The sequence reader containing the bytes to read.</param>
        /// <param name="columnMetadata">Metadata for the column.</param>
        /// <returns>An integer representing the MySQL MEDIUMINT value.</returns>
        public object ReadValue(ref SequenceReader<byte> reader, ColumnMetadata columnMetadata)
        {
            Span<byte> buffer = stackalloc byte[4];

            reader.TryCopyTo(buffer.Slice(0, 3));
            reader.Advance(3);
            
            buffer[3] = 0;

            var signalByte = buffer[2];

            if (!columnMetadata.IsUnsigned && (signalByte & 0x80) == 0x80) // Negative value
            {
                buffer[3] = 0xFF; // Set the sign bit for negative values
            }
            else
            {
                buffer[3] = 0x00; // Set the sign bit for positive values
            }

            return columnMetadata.IsUnsigned
                ? BinaryPrimitives.ReadUInt32LittleEndian(buffer)
                : BinaryPrimitives.ReadInt32LittleEndian(buffer);
        }
    }
}
