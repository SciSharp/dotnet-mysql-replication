using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Buffers;

namespace SciSharp.MySQL.Replication.Types
{
    /// <summary>
    /// Represents the MySQL ENUM data type.
    /// </summary>
    /// <remarks>
    /// Handles the reading and conversion of MySQL ENUM values.
    /// </remarks>
    class EnumType : IMySQLDataType
    {
        /// <summary>
        /// Reads an ENUM value from the binary log.
        /// </summary>
        /// <param name="reader">The sequence reader containing the bytes to read.</param>
        /// <param name="columnMetadata">Metadata for the column.</param>
        /// <returns>An integer representing the index of the ENUM value.</returns>
        public object ReadValue(ref SequenceReader<byte> reader, ColumnMetadata columnMetadata)
        {
            var enumIndex = reader.ReadInteger(2);

            // Out of range check
            if (enumIndex >= columnMetadata.EnumValues.Count)
            {
                return null;
            }

            return columnMetadata.EnumValues[enumIndex];
        }
    }
}
