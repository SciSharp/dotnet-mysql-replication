using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Buffers;

namespace SciSharp.MySQL.Replication.Types
{
    /// <summary>
    /// Represents the MySQL STRING data type.
    /// </summary>
    /// <remarks>
    /// Handles the reading and conversion of MySQL STRING values.
    /// </remarks>
    class StringType : IMySQLDataType
    {
        /// <summary>
        /// Reads a STRING value from the binary log.
        /// </summary>
        /// <param name="reader">The sequence reader containing the bytes to read.</param>
        /// <param name="meta">Metadata for the column.</param>
        /// <returns>A string representing the MySQL STRING value.</returns>
        public object ReadValue(ref SequenceReader<byte> reader, int meta)
        {
            return reader.ReadLengthEncodedString();
        }
    }
}
