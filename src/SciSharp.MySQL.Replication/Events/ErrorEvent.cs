using System;
using System.Buffers;
using System.Text;
using SuperSocket.ProtoBase;

namespace SciSharp.MySQL.Replication.Events
{
    /// <summary>
    /// Represents an error event in the MySQL binary log.
    /// Contains information about errors that occurred during the replication process.
    /// </summary>
    public sealed class ErrorEvent : LogEvent
    {
        /// <summary>
        /// Gets the error code associated with this error event.
        /// </summary>
        public short ErrorCode { get; private set; }
        
        /// <summary>
        /// Gets the SQL state code, a standard error code consisting of five characters.
        /// </summary>
        public string SqlState { get; private set; }
        
        /// <summary>
        /// Gets the human-readable error message.
        /// </summary>
        public String ErrorMessage { get; private set; }
        
        /// <summary>
        /// Decodes the body of the error event from the binary log.
        /// </summary>
        /// <param name="reader">The sequence reader containing the binary data.</param>
        /// <param name="context">The context for decoding.</param>
        protected internal override void DecodeBody(ref SequenceReader<byte> reader, object context)
        {
            reader.TryReadLittleEndian(out short errorCode);

            ErrorCode = errorCode;

            reader.TryPeek(out byte checkValue);

            if (checkValue == '#')
            {
                reader.Advance(1);
                SqlState = reader.Sequence.Slice(reader.Consumed, 5).GetString(Encoding.UTF8);
                reader.Advance(5);
            }

            ErrorMessage = reader.Sequence.Slice(reader.Consumed).GetString(Encoding.UTF8);
        }

        /// <summary>
        /// Returns a string representation of the error event.
        /// </summary>
        /// <returns>A string containing the event type, SQL state, and error message.</returns>
        public override string ToString()
        {
            return $"{EventType.ToString()}\r\nSqlState: {SqlState}\r\nErrorMessage: {ErrorMessage}";
        }
    }
}
