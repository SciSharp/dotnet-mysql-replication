using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Buffers;

namespace SciSharp.MySQL.Replication
{
    /// <summary>
    /// Represents the MySQL TIME data type.
    /// </summary>
    /// <remarks>
    /// Handles the reading and conversion of MySQL TIME values.
    /// </remarks>
    class TimeType : IMySQLDataType
    {
        /// <summary>
        /// Reads a TIME value from the binary log.
        /// </summary>
        /// <param name="reader">The sequence reader containing the bytes to read.</param>
        /// <param name="meta">Metadata for the column.</param>
        /// <returns>An object representing the MySQL TIME value.</returns>
        /// <exception cref="NotImplementedException">This method has not yet been implemented.</exception>
        public object ReadValue(ref SequenceReader<byte> reader, int meta)
        {
            throw new NotImplementedException();
        }
    }
}
