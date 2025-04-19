using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Buffers;

namespace SciSharp.MySQL.Replication.Types
{
    /// <summary>
    /// Represents the MySQL DATE data type.
    /// </summary>
    /// <remarks>
    /// Handles the reading and conversion of MySQL DATE values.
    /// </remarks>
    class DateType : IMySQLDataType
    {
        /// <summary>
        /// Reads a DATE value from the binary log.
        /// </summary>
        /// <param name="reader">The sequence reader containing the bytes to read.</param>
        /// <param name="meta">Metadata for the column.</param>
        /// <returns>A DateTime object representing the MySQL DATE value.</returns>
        public object ReadValue(ref SequenceReader<byte> reader, int meta)
        {
            // 11111000 00000000 00000000
            // 00000100 00000000 00000000
            var value = reader.ReadInteger(3);
            var day = value % 32;
            value >>= 5;
            int month = value % 16;
            int year = value >> 4;

            return new DateTime(year, month, day, 0, 0, 0);
        }
    }
}
