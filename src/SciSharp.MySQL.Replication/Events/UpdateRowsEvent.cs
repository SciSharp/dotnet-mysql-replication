using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using SuperSocket.ProtoBase;

namespace SciSharp.MySQL.Replication
{
    public sealed class UpdateRowsEvent :  RowsEvent
    {
        public class CellValue
        {
            public object OldValue { get; set; }

            public object NewValue { get; set; }
        }

        public BitArray IncludedColumnsBeforeUpdate { get; private set; }

         protected override void ReadIncludedColumns(ref SequenceReader<byte> reader)
        {
            IncludedColumnsBeforeUpdate = reader.ReadBitArray((int)reader.ReadLengthEncodedInteger());
            IncludedColumns = reader.ReadBitArray((int)reader.ReadLengthEncodedInteger());
        }

        protected override void ReadData(ref SequenceReader<byte> reader, BitArray includedColumns, TableMapEvent tableMap, int columnCount)
        {
            Rows = ReadUpdatedRows(ref reader, tableMap, IncludedColumnsBeforeUpdate, includedColumns, columnCount);
        }

        private List<object[]> ReadUpdatedRows(ref SequenceReader<byte> reader, TableMapEvent tableMap, BitArray includedColumnsBeforeUpdate, BitArray includedColumns, int columnCount)
        {
            var columnCountBeforeUpdate = GetIncludedColumnCount(IncludedColumnsBeforeUpdate);

            var rows = new List<object[]>();

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
                        
            return rows;
        }
    }
}
