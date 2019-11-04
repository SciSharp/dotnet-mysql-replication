using System;
using System.Buffers;
using System.Text;
using SuperSocket.ProtoBase;

namespace SciSharp.MySQL.Replication
{
    public sealed class QueryEvent : LogEvent
    {
        public int SlaveProxyID { get; private set; }
        public DateTime ExecutionTime { get; private set; }
        public byte SchemaLength { get; private set; }
        public short ErrorCode { get; private set; }
        public short StatusVarsLength { get; private set; }
        public string StatusVars { get; private set; }
        public string Schema { get; private set; }
        public String Query { get; private set; }
        protected internal override void DecodeBody(ref SequenceReader<byte> reader)
        {
            reader.TryReadLittleEndian(out int slaveProxyID);
            SlaveProxyID = slaveProxyID;

            reader.TryReadLittleEndian(out int seconds);
            ExecutionTime = LogEvent.GetTimestapmFromUnixEpoch(seconds);

            reader.TryRead(out byte schemaLen);
            SchemaLength = schemaLen;

            reader.TryReadLittleEndian(out short errorCode);
            ErrorCode = errorCode;

            reader.TryReadLittleEndian(out short statusVarsLen);
            StatusVarsLength = statusVarsLen;

            StatusVars = reader.Sequence.Slice(reader.Consumed, StatusVarsLength).GetString(Encoding.UTF8);
            reader.Advance(statusVarsLen);

            Schema = reader.Sequence.Slice(reader.Consumed, SchemaLength).GetString(Encoding.UTF8);
            reader.Advance(schemaLen);

            reader.Advance(1); //0x00

            if (reader.TryReadTo(out ReadOnlySequence<byte> seq, 0x00, false))
                Query = seq.GetString(Encoding.UTF8);
            else
                Query = reader.Sequence.Slice(reader.Consumed).GetString(Encoding.UTF8);
        }
    }
}
