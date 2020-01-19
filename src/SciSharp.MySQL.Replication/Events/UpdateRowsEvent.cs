using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace SciSharp.MySQL.Replication
{
    public sealed class UpdateRowsEvent :  RowsEvent
    {
        public BitArray IncludedColumnsBeforeUpdate { get; private set; }

        protected override void ReadIncludedColumns(ref SequenceReader<byte> reader)
        {
            var columnCount = (int)reader.ReadLengthEncodedInteger();
            IncludedColumnsBeforeUpdate = reader.ReadBitArray(columnCount);
            IncludedColumns = reader.ReadBitArray(columnCount);
        }

        protected override void ReadData(ref SequenceReader<byte> reader, BitArray includedColumns, TableMapEvent tableMap, int columnCount)
        {
            RowSet = ReadUpdatedRows(ref reader, tableMap, IncludedColumnsBeforeUpdate, includedColumns, columnCount);
        }

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
