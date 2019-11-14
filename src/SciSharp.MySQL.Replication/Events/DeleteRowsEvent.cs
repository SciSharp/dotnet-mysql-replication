using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using SuperSocket.ProtoBase;

namespace SciSharp.MySQL.Replication
{
    public sealed class DeleteRowsEvent :  RowsEvent
    {
        public long TableID { get; private set; }

        public BitArray IncludedColumns { get; private set; }

        public List<object[]> Rows { get; private set; }

        protected internal override void DecodeBody(ref SequenceReader<byte> reader, object context)
        {
            TableID = reader.ReadLong(6);

            reader.TryReadLittleEndian(out short flags);
            reader.TryReadLittleEndian(out short extraDataLen);
            reader.Advance(extraDataLen);

            IncludedColumns = reader.ReadBitArray((int)reader.ReadLengthEncodedInteger());

            TableMapEvent tableMap = null;

            if (context is ReplicationState repState)
                repState.TableMap.TryGetValue(TableID, out tableMap);

            if (tableMap == null)
                throw new Exception($"The table's metadata was not found: {TableID}.");

            var columnCount = GetIncludedColumnCount(IncludedColumns);

            Rows = ReadRows(ref reader, tableMap, IncludedColumns, columnCount);
        }
    }
}
