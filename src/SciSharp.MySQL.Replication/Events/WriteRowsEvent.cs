using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using SuperSocket.ProtoBase;

namespace SciSharp.MySQL.Replication
{
    public sealed class WriteRowsEvent : LogEvent
    {
        [Flags]
        public enum WriteRowsEventFlags : byte
        {
            EndOfStatement = 0x01,
            NoForeignKeyChecks = 0x02,
            NoUniqueKeyChecks = 0x04,
            RowHasAColumns = 0x08
        }

        public long TableID { get; private set; }
        public WriteRowsEventFlags WriteRowsFlags { get; private set; }

        public BitArray IncludedColumns { get; private set; }

        protected internal override void DecodeBody(ref SequenceReader<byte> reader, object context)
        {
            TableID = this.ReadLong(ref reader, 6);

            reader.TryReadLittleEndian(out short flags);
            WriteRowsFlags = (WriteRowsEventFlags)flags;

            reader.TryReadLittleEndian(out short extraDataLen);
            reader.Advance(extraDataLen);

            IncludedColumns = ReadBitmap(ref reader, (int)ReadLengthEncodedInteger(ref reader));

            TableMapEvent tableMap = null;

            if (context is ReplicationState repState)
                repState.TableMap.TryGetValue(TableID, out tableMap);

            if (tableMap == null)
                throw new Exception($"The table's metadata was not found: {TableID}.");
        }

        private List<object[]> ReadRows(ref SequenceReader<byte> reader, TableMapEvent table)
        {
            var rows = new List<object[]>();
            
            while (reader.TryPeek(out byte top) && top > 0)
            {
                var includedColumns = this.IncludedColumns;
                var columnCount = GetIncludedColumnCount(includedColumns);
                rows.Add(ReadRow(ref reader, table, includedColumns, columnCount));
            }

            reader.Advance(1);
            return rows;
        }

        private int GetIncludedColumnCount(BitArray includedColumns)
        {
            var count = 0;

            for (var i = 0; i < includedColumns.Count; i++)
            {
                if (includedColumns.Get(i))
                    count++;
            }
        
            return count;
        }

        private object[] ReadRow(ref SequenceReader<byte> reader, TableMapEvent table, BitArray includedColumns, int columnCount)
        {
            var cells = new object[columnCount];
            var nullColumns = ReadBitmap(ref reader, columnCount, true);
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
            switch (columnType)
            {
                case ColumnType.BIT:
                    int bitSetLength = (meta >> 8) * 8 + (meta & 0xFF);
                    return ReadBitmap(ref reader, bitSetLength);

                case ColumnType.TINY:
                    return (byte)ReadLong(ref reader, 1);

                case ColumnType.SHORT:
                    return (short)ReadLong(ref reader, 2);

                case ColumnType.INT24:
                    return (int)ReadLong(ref reader, 3);

                case ColumnType.LONG:
                    return ReadLong(ref reader, 4);

                case ColumnType.LONGLONG:
                    return ReadLong(ref reader, 8);

                case ColumnType.FLOAT:
                    return ReadLong(ref reader, 4); // INT to float?
                default:
                    throw new NotImplementedException($"The reading for the type {columnType} has not been implemented yet.");
            }
        }
    }
}
