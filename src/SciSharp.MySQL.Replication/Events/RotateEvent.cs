using System;
using System.Buffers;
using System.Text;
using SuperSocket.ProtoBase;

namespace SciSharp.MySQL.Replication
{
    /// <summary>
    /// Represents a MySQL ROTATE_EVENT that indicates a switch to a new binary log file.
    /// This event is generated when the master server switches to a new binary log file,
    /// either because the current file reached the max size or due to a manual rotation.
    /// https://dev.mysql.com/doc/internals/en/rotate-event.html
    /// </summary>
    public sealed class RotateEvent : LogEvent
    {
        /// <summary>
        /// Gets or sets the position in the new binary log file where the next event starts.
        /// </summary>
        public long RotatePosition { get; set; }

        /// <summary>
        /// Gets or sets the name of the new binary log file.
        /// </summary>
        public string NextBinlogFileName { get; set; }

        /// <summary>
        /// Decodes the body of the event from the binary representation.
        /// </summary>
        /// <param name="reader">The sequence reader containing the binary data.</param>
        /// <param name="context">The context for decoding.</param>
        protected internal override void DecodeBody(ref SequenceReader<byte> reader, object context)
        {
            reader.TryReadLittleEndian(out long position);
            RotatePosition = position;

            var binglogFileNameSize = reader.Remaining - (int)LogEvent.ChecksumType;

            NextBinlogFileName = reader.Sequence.Slice(reader.Consumed, binglogFileNameSize).GetString(Encoding.UTF8);
            reader.Advance(binglogFileNameSize);
        }

        /// <summary>
        /// Returns a string representation of the RotateEvent.
        /// </summary>
        /// <returns>A string containing the event type, new filename, and position information.</returns>
        public override string ToString()
        {
            return $"{EventType.ToString()}\r\nRotatePosition: {RotatePosition}\r\nNextBinlogFileName: {NextBinlogFileName}";
        }
    }
}
