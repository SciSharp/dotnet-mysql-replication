using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Buffers;

namespace SciSharp.MySQL.Replication
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
        /// <param name="meta">Metadata that describes the data format.</param>
        /// <returns>The deserialized object value.</returns>
        object ReadValue(ref SequenceReader<byte> reader, int meta);
    }
}
