using System;
using System.Buffers;
using System.Collections;
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
        public DateTime ExecutionTime { get; private set; }
        public byte SchemaLength { get; private set; }
        public short ErrorCode { get; private set; }
        public short StatusVarsLength { get; private set; }
        public string StatusVars { get; private set; }
        public string Schema { get; private set; }
        public String Query { get; private set; }
        protected internal override void DecodeBody(ref SequenceReader<byte> reader, object context)
        {
            TableID = this.ReadLong(ref reader, 6);

            reader.TryReadLittleEndian(out short flags);
            WriteRowsFlags = (WriteRowsEventFlags)flags;

            reader.TryReadLittleEndian(out short extraDataLen);
            reader.Advance(extraDataLen);

            var columnCount = ReadLengthEncodedInteger(ref reader);
            var columnBitmap = ReadBitmap(ref reader, (int)columnCount);

            TableMapEvent tableMap;

            if (context is ReplicationState repState)
            {
                tableMap = repState.CurrentTableMap;
            }
        }        
    }
}
