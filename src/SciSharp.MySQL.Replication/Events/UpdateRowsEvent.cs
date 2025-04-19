using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace SciSharp.MySQL.Replication
{
    /// <summary>
    /// Represents a MySQL binary log event that contains rows updated in a table.
    /// This event is generated for an update operation on rows in a MySQL table and
    /// contains both the before and after values of the affected rows.
    /// </summary>
    public sealed class UpdateRowsEvent :  RowsEvent
    {
        /// <summary>
        /// Gets the bitmap indicating which columns are included in the before-update image.
        /// </summary>
        public BitArray IncludedColumnsBeforeUpdate { get; private set; }

        /// <summary>
        /// Reads the included columns bitmaps from the binary representation.
        /// For update events, this includes both the before and after update column bitmaps.
        /// </summary>
        /// <param name="reader">The sequence reader containing the binary data.</param>
        protected override void ReadIncludedColumns(ref SequenceReader<byte> reader)
        {
            var columnCount = (int)reader.ReadLengthEncodedInteger();
            IncludedColumnsBeforeUpdate = reader.ReadBitArray(columnCount);
            IncludedColumns = reader.ReadBitArray(columnCount);
        }

        /// <summary>
        /// Reads the row data from the binary representation, including both
        /// before and after values for updated rows.
        /// </summary>
        /// <param name="reader">The sequence reader containing the binary data.</param>
        /// <param name="includedColumns">The bitmap of columns present in the row data.</param>
        /// <param name="tableMap">The table mapping information.</param>
        /// <param name="columnCount">The number of columns.</param>
        protected override void ReadData(ref SequenceReader<byte> reader, BitArray includedColumns, TableMapEvent tableMap, int columnCount)
        {
            RowSet = ReadUpdatedRows(ref reader, tableMap, IncludedColumnsBeforeUpdate, includedColumns, columnCount);
        }

        /// <summary>
        /// Reads the updated rows data from the binary representation.
        /// </summary>
        /// <param name="reader">The sequence reader containing the binary data.</param>
        /// <param name="tableMap">The table mapping information.</param>
        /// <param name="includedColumnsBeforeUpdate">The bitmap of columns included in the before-update image.</param>
        /// <param name="includedColumns">The bitmap of columns included in the after-update image.</param>
        /// <param name="columnCount">The number of columns.</param>
        /// <returns>A RowSet containing the updated rows data with both before and after values.</returns>
        private RowSet ReadUpdatedRows(ref SequenceReader<byte> reader, TableMapEvent tableMap, BitArray includedColumnsBeforeUpdate, BitArray includedColumns, int columnCount)
        {
            var columnCountBeforeUpdate = GetIncludedColumnCount(IncludedColumnsBeforeUpdate);

            var rows = new List<object[]>();
            var columns = GetColumnNames(tableMap, includedColumnsBeforeUpdate, columnCount);

            while (reader.Remaining > 0)
            {
                var oldCellValues = ReadRow(ref reader, tableMap, includedColumnsBeforeUpdate, columnCountBeforeUpdate);
                var newCellValues = ReadRow(ref reader, tableMap, includedColumnsBeforeUpdate, columnCount);

                var cellCount = Math.Min(oldCellValues.Length, newCellValues.Length);
                var cells = new object[cellCount];

                for (var i = 0; i < cellCount; i++)
                {
                    cells[i] = new CellValue
                    {
                        OldValue = oldCellValues[i],
                        NewValue = newCellValues[i]
                    };
                }

                rows.Add(cells);
            }
                        
            return new RowSet
            {
                ColumnNames = columns,
                Rows = rows
            };
        }

        /// <summary>
        /// Returns a string representation of the UpdateRowsEvent.
        /// </summary>
        /// <returns>A string containing the event type, table ID, and before/after row values.</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append($"{EventType.ToString()}\r\nTableID: {TableID}");

            var columns = IncludedColumns;
            var rows = RowSet.Rows;
            var columnNames = RowSet.ColumnNames;

            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i];

                for (int j = 0; j < row.Length; j++)
                {                    
                    var name = columnNames?[i];
                    var value = row[j++] as CellValue;

                    if (string.IsNullOrEmpty(name))
                        sb.Append($"\r\n{value.OldValue}=>{value.NewValue}");
                    else
                        sb.Append($"\r\n{name}: {value.OldValue}=>{value.NewValue}");
                }
            }

            return sb.ToString();          
        }
    }
}
