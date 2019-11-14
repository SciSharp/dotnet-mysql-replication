using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using SuperSocket.ProtoBase;

namespace SciSharp.MySQL.Replication
{
    public abstract class RowsEvent : LogEvent
    {
        protected List<object[]> ReadRows(ref SequenceReader<byte> reader, TableMapEvent table, BitArray includedColumns, int columnCount)
        {
            var rows = new List<object[]>();
            
            while (reader.Remaining > 0)
            {
                rows.Add(ReadRow(ref reader, table, includedColumns, columnCount));
            }

            return rows;
        }

        protected int GetIncludedColumnCount(BitArray includedColumns)
        {
            var count = 0;

            for (var i = 0; i < includedColumns.Count; i++)
            {
                if (includedColumns.Get(i))
                    count++;
            }
        
            return count;
        }

        protected object[] ReadRow(ref SequenceReader<byte> reader, TableMapEvent table, BitArray includedColumns, int columnCount)
        {
            var cells = new object[columnCount];
            var nullColumns = reader.ReadBitArray(columnCount, true);
            var columnTypes = table.ColumnTypes;
            var columnMetadata = table.ColumnMetadata;

            for (int i = 0, numberOfSkippedColumns = 0; i < columnTypes.Length; i++)
            {
                if (!includedColumns.Get(i))
                {
                    numberOfSkippedColumns++;
                    continue;
                }

                int index = i - numberOfSkippedColumns;

                if (!nullColumns.Get(index))
                    continue;

                var typeCode = columnTypes[i];
                var meta = columnMetadata[i];
                var length = 0;

                var columnType = (ColumnType)typeCode;

                if (columnType == ColumnType.STRING)
                {
                    if (meta >= 256)
                    {
                        int meta0 = meta >> 8, meta1 = meta & 0xFF;
                        if ((meta0 & 0x30) != 0x30)
                        {
                            typeCode = (byte)(meta0 | 0x30);
                            columnType = (ColumnType)typeCode;
                            length = meta1 | (((meta0 & 0x30) ^ 0x30) << 4);
                        }
                        else
                        {
                            // mysql-5.6.24 sql/rpl_utility.h enum_field_types (line 278)
                            if (meta0 == (int)ColumnType.ENUM || meta0 == (int)ColumnType.SET)
                            {
                                typeCode = (byte)meta0;
                                columnType = (ColumnType)typeCode;
                            }
                            length = meta1;
                        }
                    }
                    else
                    {
                        length = meta;
                    }
                }

                cells[index] = ReadCell(ref reader, columnType, meta, length);
            }

            return cells;
        }

        private object ReadCell(ref SequenceReader<byte> reader, ColumnType columnType, int meta, int length)
        {
            var dataType = DataTypes[(int)columnType] as IMySQLDataType;

            if (dataType == null)
                throw new NotImplementedException();

            return dataType.ReadValue(ref reader, meta);
        }
    }
}
