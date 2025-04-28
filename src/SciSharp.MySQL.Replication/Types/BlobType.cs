using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Buffers;

namespace SciSharp.MySQL.Replication.Types
{
    /// <summary>
    /// Represents the MySQL BLOB data type.
    /// </summary>
    /// <remarks>
    /// Handles the reading and conversion of MySQL BLOB values.
    /// </remarks>
    class BlobType : IMySQLDataType
    {
        /// <summary>
        /// Reads a BLOB value from the binary log.
        /// </summary>
        /// <param name="reader">The sequence reader containing the bytes to read.</param>
        /// <param name="columnMetadata">Metadata for the column.</param>
        /// <returns>A byte array representing the MySQL BLOB value.</returns>
        public object ReadValue(ref SequenceReader<byte> reader, ColumnMetadata columnMetadata)
        {
            int blobLength = reader.ReadInteger(columnMetadata.MetadataValue[0]);

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
