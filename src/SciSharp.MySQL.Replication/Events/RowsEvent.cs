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
        public RowsEvent()
        {
            HasCRC = true;
        }

        public long TableID { get; protected set; }

        public RowsEventFlags RowsEventFlags { get; protected set; }

        public BitArray IncludedColumns { get; protected set; }

        public RowSet RowSet { get; protected set; }

        protected TableMapEvent TableMap { get; private set; }

        protected internal override void DecodeBody(ref SequenceReader<byte> reader, object context)
        {
            TableID = reader.ReadLong(6);

            reader.TryReadLittleEndian(out short flags);
            RowsEventFlags = (RowsEventFlags)flags;

            reader.TryReadLittleEndian(out short extraDataLen);
            reader.Advance(extraDataLen - 2);

            ReadIncludedColumns(ref reader);

            TableMapEvent tableMap = null;

            if (context is ReplicationState repState)
            {
                if (repState.TableMap.TryGetValue(TableID, out tableMap))
                {
                    TableMap = tableMap;
                }
            }

            if (tableMap == null)
                throw new Exception($"The table's metadata was not found: {TableID}.");

            var columnCount = GetIncludedColumnCount(IncludedColumns);
            
            RebuildReaderAsCRC(ref reader);
            ReadData(ref reader, IncludedColumns, tableMap, columnCount);            
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append($"{EventType.ToString()}\r\nTableID: {TableID}");

            var rowSet = RowSet;
            var columnNames = rowSet.ColumnNames;

            for (var i = 0; i < rowSet.Rows.Count; i++)
            {
                var row = rowSet.Rows[i];

                for (int j = 0; j < row.Length; j++)
                {
                    var name = columnNames?[j];
                    var value = row[j++];

                    if (string.IsNullOrEmpty(name))
                        sb.Append($"\r\n{value}");
                    else               
                        sb.Append($"\r\n{name}: {value}");
                }
            }

            return sb.ToString();          
        }

        protected virtual void ReadIncludedColumns(ref SequenceReader<byte> reader)
        {
            IncludedColumns = reader.ReadBitArray((int)reader.ReadLengthEncodedInteger());
        }

        protected virtual void ReadData(ref SequenceReader<byte> reader, BitArray includedColumns, TableMapEvent tableMap, int columnCount)
        {
            RowSet = ReadRows(ref reader, tableMap, IncludedColumns, columnCount);
        }

        protected IReadOnlyList<string> GetColumnNames(TableMapEvent table, BitArray includedColumns, int columnCount)
        {
            var columns = new List<string>(columnCount);
            var columnNames = table.Metadata.ColumnNames;

            if (columnNames != null && columnNames.Count > 0)
            {
                for (var i = 0; i < includedColumns.Count; i++)
                {
                    if (!includedColumns.Get(i))
                        continue;

                    columns.Add(columnNames[i]);
                }
            }

            return columns;
        }

        protected RowSet ReadRows(ref SequenceReader<byte> reader, TableMapEvent table, BitArray includedColumns, int columnCount)
        {
            var rows = new List<object[]>();
            var columns = GetColumnNames(table, includedColumns, columnCount);
            
            while (reader.Remaining > 0)
            {
                rows.Add(ReadRow(ref reader, table, includedColumns, columnCount));
            }

            return new RowSet
            {
                Rows = rows,
                ColumnNames = columns
            };
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

                if (nullColumns.Get(index))
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
