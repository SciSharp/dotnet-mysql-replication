using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Buffers;

namespace SciSharp.MySQL.Replication.Types
{
    /// <summary>
    /// Represents the MySQL BIT data type.
    /// </summary>
    /// <remarks>
    /// Handles the reading and conversion of MySQL BIT values.
    /// </remarks>
    class BitType : IMySQLDataType
    {
        /// <summary>
        /// Reads a BIT value from the binary log.
        /// </summary>
        /// <param name="reader">The sequence reader containing the bytes to read.</param>
        /// <param name="meta">Metadata for the column.</param>
        /// <returns>An object representing the MySQL BIT value.</returns>
        public object ReadValue(ref SequenceReader<byte> reader, int meta)
        {
            int bitArrayLength = (meta >> 8) * 8 + (meta & 0xFF);
            return reader.ReadBitArray(bitArrayLength, false);
        }
    }
}
