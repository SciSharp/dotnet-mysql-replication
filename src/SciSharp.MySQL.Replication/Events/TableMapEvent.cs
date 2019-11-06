using System;
using System.Buffers;
using System.Collections;
using System.Text;
using SuperSocket.ProtoBase;

namespace SciSharp.MySQL.Replication
{
    /// <summary>
    /// https://dev.mysql.com/doc/internals/en/table-map-event.html
    /// </summary>
    public sealed class TableMapEvent : LogEvent
    {
        public long TableID { get; set; }

        public string SchemaName { get; set; }

        public string TableName { get; set; }

        public int ColumnCount { get; set; }

        public string ColumnDef { get; set; }

        public string ColumnMetaDef { get; set; }

        public BitArray NullBitmap { get; set; }

        protected internal override void DecodeBody(ref SequenceReader<byte> reader)
        {
            TableID = ReadLong(ref reader, 6);

            reader.Advance(2); // skip flags

            byte len;
            reader.TryRead(out len);
            SchemaName = ReadString(ref reader, len);

            reader.TryRead(out len);
            TableName = ReadString(ref reader, len);

            ColumnCount = (int)ReadLengthEncodedInteger(ref reader);
            ColumnDef = ReadString(ref reader, ColumnCount);
            ColumnMetaDef = ReadString(ref reader, (int)ReadLengthEncodedInteger(ref reader));
            
            NullBitmap = ReadBitmap(ref reader, ColumnCount);
        }
    }
}
