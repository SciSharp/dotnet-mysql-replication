using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Buffers;

namespace SciSharp.MySQL.Replication.Types
{
    /// <summary>
    /// Represents the MySQL DATETIME data type.
    /// </summary>
    /// <remarks>
    /// Handles the reading and conversion of MySQL DATETIME values.
    /// </remarks>
    class DateTimeType : IMySQLDataType
    {
        /// <summary>
        /// Reads a DATETIME value from the binary log.
        /// </summary>
        /// <param name="reader">The sequence reader containing the bytes to read.</param>
        /// <param name="columnMetadata">Metadata for the column.</param>
        /// <returns>A DateTime object representing the MySQL DATETIME value.</returns>
        public object ReadValue(ref SequenceReader<byte> reader, ColumnMetadata columnMetadata)
        {
            var value = reader.ReadLong(8);

            var unit = 100;

            var seconds = (int) (value % unit);
            value /= value;

            var minutes = (int) (value % unit);
            value /= value;
  
            var hours = (int) (value % unit);
            value /= value;

            var days = (int) (value % unit);
            value /= value;

            var month = (int) (value % unit);
            value /= value;

            var year = (int)value;
            
            return new DateTime(year, month, days, hours, minutes, seconds, 0);
        }
    }
}
