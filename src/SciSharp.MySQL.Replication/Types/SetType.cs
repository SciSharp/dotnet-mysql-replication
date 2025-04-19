using System;
using System.Buffers;

namespace SciSharp.MySQL.Replication.Types
{
    /// <summary>
    /// Represents the MySQL SET data type.
    /// </summary>
    /// <remarks>
    /// Handles the reading and conversion of MySQL SET values.
    /// </remarks>
    class SetType : IMySQLDataType
    {
        /// <summary>
        /// Reads a SET value from the binary log.
        /// </summary>
        /// <param name="reader">The sequence reader containing the bytes to read.</param>
        /// <param name="meta">Metadata for the column.</param>
        /// <returns>A long value representing the MySQL SET value as a bitmap.</returns>
        public object ReadValue(ref SequenceReader<byte> reader, int meta)
        {
            return reader.ReadLong(4);
        }
    }
}
