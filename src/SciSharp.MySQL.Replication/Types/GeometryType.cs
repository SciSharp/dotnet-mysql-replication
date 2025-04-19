using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Buffers;

namespace SciSharp.MySQL.Replication
{
    /// <summary>
    /// Represents the MySQL GEOMETRY data type.
    /// </summary>
    /// <remarks>
    /// Handles the reading and conversion of MySQL GEOMETRY values.
    /// </remarks>
    class GeometryType : IMySQLDataType
    {
        /// <summary>
        /// Reads a GEOMETRY value from the binary log.
        /// </summary>
        /// <param name="reader">The sequence reader containing the bytes to read.</param>
        /// <param name="meta">Metadata for the column.</param>
        /// <returns>A byte array representing the MySQL GEOMETRY value.</returns>
        public object ReadValue(ref SequenceReader<byte> reader, int meta)
        {
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
