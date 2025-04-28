using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Buffers;
using SuperSocket.ProtoBase;
using System.Text;
using System.Buffers.Binary;

namespace SciSharp.MySQL.Replication.Types
{
    /// <summary>
    /// Represents the MySQL VARCHAR data type.
    /// </summary>
    /// <remarks>
    /// Handles the reading and conversion of MySQL VARCHAR values.
    /// </remarks>
    class VarCharType : IMySQLDataType, IColumnMetadataLoader
    {
        public void LoadMetadataValue(ColumnMetadata columnMetadata)
        {
            if (columnMetadata.MetadataValue.Length == 1)
            {
                columnMetadata.MaxLength = (int)columnMetadata.MetadataValue[0];
            }
            else
            {
                columnMetadata.MaxLength = (int)columnMetadata.MetadataValue[1] * 256 + (int)columnMetadata.MetadataValue[0];
            }
        }

        /// <summary>
        /// Reads a VARCHAR value from the binary log.
        /// </summary>
        /// <param name="reader">The sequence reader containing the bytes to read.</param>
        /// <param name="columnMetadata">Metadata for the column.</param>
        /// <returns>A string representing the MySQL VARCHAR value.</returns>
        public object ReadValue(ref SequenceReader<byte> reader, ColumnMetadata columnMetadata)
        {
            var lenBytes = columnMetadata.MaxLength < 256 ? 1 : 2;
            var length = lenBytes == 1
                ? reader.TryRead(out byte len1) ? len1 : 0
                : reader.TryReadLittleEndian(out short len2) ? len2 : 0;

            if (length == 0)
                return string.Empty;
            
            try
            {
                if (lenBytes == 1)
                {
                    if (reader.TryPeek(out byte checkByte) && checkByte == 0x00)
                    {
                        reader.Advance(1);
                    }
                }

                return reader.UnreadSequence.Slice(0, length).GetString(Encoding.UTF8);
            }
            finally
            {
                reader.Advance(length);
            }
        }
    }
}
