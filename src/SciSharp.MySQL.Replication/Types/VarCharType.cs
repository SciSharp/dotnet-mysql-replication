using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Buffers;

namespace SciSharp.MySQL.Replication.Types
{
    /// <summary>
    /// Represents the MySQL VARCHAR data type.
    /// </summary>
    /// <remarks>
    /// Handles the reading and conversion of MySQL VARCHAR values.
    /// </remarks>
    class VarCharType : IMySQLDataType
    {
        /// <summary>
        /// Reads a VARCHAR value from the binary log.
        /// </summary>
        /// <param name="reader">The sequence reader containing the bytes to read.</param>
        /// <param name="meta">Metadata for the column.</param>
        /// <returns>A string representing the MySQL VARCHAR value.</returns>
        public object ReadValue(ref SequenceReader<byte> reader, int meta)
        {
            return reader.ReadLengthEncodedString();
        }
    }
}
