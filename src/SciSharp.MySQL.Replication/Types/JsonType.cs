using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Buffers;

namespace SciSharp.MySQL.Replication.Types
{
    /// <summary>
    /// Represents the MySQL JSON data type.
    /// </summary>
    /// <remarks>
    /// Handles the reading and conversion of MySQL JSON values.
    /// </remarks>
    class JsonType : IMySQLDataType
    {
        /// <summary>
        /// Reads a JSON value from the binary log.
        /// </summary>
        /// <param name="reader">The sequence reader containing the bytes to read.</param>
        /// <param name="columnMetadata">Metadata for the column.</param>
        /// <returns>A byte array representing the MySQL JSON value.</returns>
        public object ReadValue(ref SequenceReader<byte> reader, ColumnMetadata columnMetadata)
        {
            int meta = columnMetadata.MetadataValue[0];
            int blobLength = reader.ReadInteger(meta);

            try
            {
                return reader.Sequence.Slice(reader.Consumed, blobLength).ToArray();
            }
            finally
            {
                reader.Advance(blobLength);
            }
        }
    }
}
