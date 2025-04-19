using System;
using System.Buffers;

namespace SciSharp.MySQL.Replication.Events
{
    /// <summary>
    /// Represents a placeholder for log event types that have not been implemented yet.
    /// This event is used when the system encounters an event type that it recognizes
    /// but doesn't have specific handling logic for.
    /// </summary>
    public sealed class NotImplementedEvent : LogEvent
    {
        /// <summary>
        /// Decodes the body of the not implemented event. This implementation is intentionally
        /// empty as the event doesn't process any content.
        /// </summary>
        /// <param name="reader">The sequence reader containing the binary data.</param>
        /// <param name="context">The context for decoding.</param>
        protected internal override void DecodeBody(ref SequenceReader<byte> reader, object context)
        {

        }
    }
}
