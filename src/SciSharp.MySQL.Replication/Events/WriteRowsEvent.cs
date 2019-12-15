using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using SuperSocket.ProtoBase;

namespace SciSharp.MySQL.Replication
{
    public sealed class WriteRowsEvent :  RowsEvent
    {
        [Flags]
        public enum WriteRowsEventFlags : byte
        {
            EndOfStatement = 0x01,
            NoForeignKeyChecks = 0x02,
            NoUniqueKeyChecks = 0x04,
            RowHasAColumns = 0x08
        }

        public WriteRowsEvent()
        {
            HasCRC = true;
        }

        public long TableID { get; private set; }

        public WriteRowsEventFlags WriteRowsFlags { get; private set; }

        public BitArray IncludedColumns { get; private set; }

        public List<object[]> Rows { get; private set; }

        protected internal override void DecodeBody(ref SequenceReader<byte> reader, object context)
        {
            TableID = reader.ReadLong(6);

            reader.TryReadLittleEndian(out short flags);
            WriteRowsFlags = (WriteRowsEventFlags)flags;

            reader.TryReadLittleEndian(out short extraDataLen);
            reader.Advance(extraDataLen - 2);

            var includecColumnLen = (int)reader.ReadLengthEncodedInteger();

            IncludedColumns = reader.ReadBitArray(includecColumnLen);

            TableMapEvent tableMap = null;

            if (context is ReplicationState repState)
                repState.TableMap.TryGetValue(TableID, out tableMap);

            if (tableMap == null)
                throw new Exception($"The table's metadata was not found: {TableID}.");

            var columnCount = GetIncludedColumnCount(IncludedColumns);
            
            RebuildReaderAsCRC(ref reader);

            Rows = ReadRows(ref reader, tableMap, IncludedColumns, columnCount);
        }
    }
}
