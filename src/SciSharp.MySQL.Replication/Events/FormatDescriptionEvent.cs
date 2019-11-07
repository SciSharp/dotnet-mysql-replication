using System;
using System.Buffers;
using System.Text;
using SuperSocket.ProtoBase;

namespace SciSharp.MySQL.Replication
{
    /// <summary>
    /// https://dev.mysql.com/doc/internals/en/format-description-event.html
    /// </summary>
    public sealed class FormatDescriptionEvent : LogEvent
    {
        public short BinlogVersion { get; set; }

        public string ServerVersion { get; set; }

        public DateTime CreateTimestamp { get; set; }

        public byte EventHeaderLength { get; set; }

        public byte[] EventTypeHeaderLengths { get; set; }

        private string ReadServerVersion(ref SequenceReader<byte> reader, int len)
        {
            ReadOnlySequence<byte> seq;

            if (reader.TryReadTo(out seq, 0x00, false))
            {
                if (seq.Length > len)                    
                {
                    seq = seq.Slice(0, len);
                    reader.Rewind(len - seq.Length);
                }

                var version = seq.GetString(Encoding.UTF8);

                if (seq.Length < len)
                    reader.Advance(len - seq.Length);
                
                return version;
            }
            else
            {
                seq = reader.Sequence.Slice(reader.Consumed, len);
                var version = seq.GetString(Encoding.UTF8);
                reader.Advance(len);

                return version;
            }
        }

        protected internal override void DecodeBody(ref SequenceReader<byte> reader, object context)
        {
            reader.TryReadLittleEndian(out short version);
            BinlogVersion = version;

            ServerVersion = ReadServerVersion(ref reader, 50);     

            reader.TryReadLittleEndian(out int seconds);
            CreateTimestamp = LogEvent.GetTimestapmFromUnixEpoch(seconds);

            reader.TryRead(out byte eventLen);
            EventHeaderLength = eventLen;

            var eventTypeHeaderLens = new byte[reader.Remaining];

            for (var i = 0; i < eventTypeHeaderLens.Length; i++)
            {
                reader.TryRead(out eventLen);
                eventTypeHeaderLens[i] = eventLen;
            }

            EventTypeHeaderLengths = eventTypeHeaderLens;            
        }
    }
}
