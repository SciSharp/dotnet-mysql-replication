using System;
using System.Buffers;
using System.Text;
using SuperSocket.ProtoBase;

namespace SciSharp.MySQL.Replication.Events
{
    /// <summary>
    /// Represents a MySQL FORMAT_DESCRIPTION_EVENT that describes the format of the binary log.
    /// This event appears at the beginning of each binary log file and provides information
    /// about the server version and header lengths for each event type.
    /// https://dev.mysql.com/doc/internals/en/format-description-event.html
    /// </summary>
    public sealed class FormatDescriptionEvent : LogEvent
    {
        /// <summary>
        /// Gets or sets the binary log format version.
        /// </summary>
        public short BinlogVersion { get; set; }

        /// <summary>
        /// Gets or sets the MySQL server version string.
        /// </summary>
        public string ServerVersion { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the binary log was created.
        /// </summary>
        public DateTime CreateTimestamp { get; set; }

        /// <summary>
        /// Gets or sets the length of the event header.
        /// </summary>
        public byte EventHeaderLength { get; set; }

        /// <summary>
        /// Gets or sets the array of event type header lengths.
        /// Each index corresponds to a LogEventType and contains the header length for that type.
        /// </summary>
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

        /// <summary>
        /// Decodes the body of the event from the binary representation.
        /// </summary>
        /// <param name="reader">The sequence reader containing the binary data.</param>
        /// <param name="context">The context for decoding.</param>
        protected internal override void DecodeBody(ref SequenceReader<byte> reader, object context)
        {
            reader.TryReadLittleEndian(out short version);
            BinlogVersion = version;

            ServerVersion = ReadServerVersion(ref reader, 50);     

            reader.TryReadLittleEndian(out int seconds);
            CreateTimestamp = LogEvent.GetTimestampFromUnixEpoch(seconds);

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

        /// <summary>
        /// Returns a string representation of the FormatDescriptionEvent.
        /// </summary>
        /// <returns>A string containing event information.</returns>
        public override string ToString()
        {
            return $"{EventType.ToString()}\r\nBinlogVersion: {BinlogVersion}\r\nServerVersion: {ServerVersion}";
        }
    }
}
