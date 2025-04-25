using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Buffers;

namespace SciSharp.MySQL.Replication.Types
{
    /// <summary>
    /// Interface that defines MySQL data type read operations.
    /// </summary>
    internal interface IMySQLDataType
    {
        /// <summary>
        /// Reads a value from the sequence reader based on the metadata.
        /// </summary>
        /// <param name="reader">The sequence reader containing binary data.</param>
        /// <param name="columnMetadata">Metadata for the column being read.</param>
        /// <returns>The deserialized object value.</returns>
        object ReadValue(ref SequenceReader<byte> reader, ColumnMetadata columnMetadata);
    }
}
