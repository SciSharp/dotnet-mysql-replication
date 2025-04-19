using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Buffers;
using System.Globalization;

namespace SciSharp.MySQL.Replication
{
    /// <summary>
    /// Represents the MySQL DECIMAL data type.
    /// </summary>
    /// <remarks>
    /// Handles the reading and conversion of MySQL DECIMAL values.
    /// </remarks>
    class NewDecimalType : IMySQLDataType
    {
        /// <summary>
        /// Reads a DECIMAL value from the binary log.
        /// </summary>
        /// <param name="reader">The sequence reader containing the bytes to read.</param>
        /// <param name="meta">Metadata for the column.</param>
        /// <returns>A decimal value representing the MySQL DECIMAL value.</returns>
        public object ReadValue(ref SequenceReader<byte> reader, int meta)
        {
            return decimal.Parse(reader.ReadLengthEncodedString(), CultureInfo.InvariantCulture);
        }
    }
}
