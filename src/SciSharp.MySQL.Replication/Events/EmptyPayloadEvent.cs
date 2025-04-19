using System;
using System.Buffers;

namespace SciSharp.MySQL.Replication.Events
{
    /// <summary>
    /// Represents a log event with an empty payload.
    /// This is used for events that don't contain any additional data beyond their headers.
    /// </summary>
    public sealed class EmptyPayloadEvent : LogEvent
    {
        /// <summary>
        /// Decodes the body of the empty payload event, which contains no data.
        /// </summary>
        /// <param name="reader">The sequence reader containing the binary data.</param>
        /// <param name="context">The context for decoding.</param>
        protected internal override void DecodeBody(ref SequenceReader<byte> reader, object context)
        {
            
        }
    }
}
