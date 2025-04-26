using System;
using System.Buffers;
using System.Collections.Generic;

namespace SciSharp.MySQL.Replication.Types
{
    /// <summary>
    /// Represents the MySQL SET data type.
    /// </summary>
    /// <remarks>
    /// Handles the reading and conversion of MySQL SET values.
    /// </remarks>
    class SetType : IMySQLDataType, IColumnMetadataLoader
    {
        public void LoadMetadataValue(ColumnMetadata columnMetadata)
        {
            columnMetadata.MaxLength = columnMetadata.MetadataValue & 0xFF;
        }

        /// <summary>
        /// Reads a SET value from the binary log.
        /// </summary>
        /// <param name="reader">The sequence reader containing the bytes to read.</param>
        /// <param name="columnMetadata">Metadata for the column.</param>
        /// <returns>A long value representing the MySQL SET value as a bitmap.</returns>
        public object ReadValue(ref SequenceReader<byte> reader, ColumnMetadata columnMetadata)
        {
            var flags = reader.ReadLong(columnMetadata.MaxLength);

            if (flags == 0)
            {
                return string.Empty;
            }

            var setCellValues = new List<string>();

            for (int i = 0; i < columnMetadata.SetValues.Count; i++)
            {
                if ((flags & (1 << i)) != 0)
                {
                    setCellValues.Add(columnMetadata.SetValues[i]);
                }
            }

            return string.Join(",", setCellValues);
        }
    }
}
