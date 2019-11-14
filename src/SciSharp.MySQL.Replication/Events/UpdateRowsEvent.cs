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
        public long TableID { get; private set; }

        public BitArray IncludedColumnsBeforeUpdate { get; private set; }

        public BitArray IncludedColumns { get; private set; }

        public List<Tuple<object[], object[]>> Rows { get; private set; }

        protected internal override void DecodeBody(ref SequenceReader<byte> reader, object context)
        {
            TableID = reader.ReadLong(6);

            reader.TryReadLittleEndian(out short flags);
            reader.TryReadLittleEndian(out short extraDataLen);
            reader.Advance(extraDataLen);

            IncludedColumnsBeforeUpdate = reader.ReadBitArray((int)reader.ReadLengthEncodedInteger());
            IncludedColumns = reader.ReadBitArray((int)reader.ReadLengthEncodedInteger());

            TableMapEvent tableMap = null;

            if (context is ReplicationState repState)
                repState.TableMap.TryGetValue(TableID, out tableMap);

            if (tableMap == null)
                throw new Exception($"The table's metadata was not found: {TableID}.");    

            Rows = ReadUpdatedRows(ref reader, tableMap, IncludedColumnsBeforeUpdate, IncludedColumns);
        }

        private List<Tuple<object[], object[]>> ReadUpdatedRows(ref SequenceReader<byte> reader, TableMapEvent tableMap, BitArray includedColumnsBeforeUpdate, BitArray includedColumns)
        {
            var columnCountBeforeUpdate = GetIncludedColumnCount(IncludedColumnsBeforeUpdate);
            var columnCount = GetIncludedColumnCount(IncludedColumns);

            var rows = new List<Tuple<object[], object[]>>();

            while (reader.Remaining > 0)
            {
                rows.Add(Tuple.Create(
                        ReadRow(ref reader, tableMap, includedColumnsBeforeUpdate, columnCountBeforeUpdate),
                        ReadRow(ref reader, tableMap, includedColumns, columnCount))
                    );
            }
                        
            return rows;
        }
    }
}
