using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Buffers;

namespace SciSharp.MySQL.Replication
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
        /// <param name="meta">Metadata for the column.</param>
        /// <returns>An integer representing the MySQL MEDIUMINT value.</returns>
        public object ReadValue(ref SequenceReader<byte> reader, int meta)
        {
            return reader.ReadInteger(3);
        }
    }
}
