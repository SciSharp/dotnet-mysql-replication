using System;
using System.Buffers;

namespace SciSharp.MySQL.Replication.Events
{
    /// <summary>
    /// Represents a MySQL XID_EVENT that marks the end of a transaction that modifies data.
    /// This event contains the transaction ID used for the binary log.
    /// </summary>
    public sealed class XIDEvent : LogEvent
    {
        /// <summary>
        /// Gets or sets the ID of the transaction.
        /// </summary>
        public long TransactionID { get; set; }
        
        /// <summary>
        /// Decodes the body of the XID event from the binary representation.
        /// </summary>
        /// <param name="reader">The sequence reader containing the binary data.</param>
        /// <param name="context">The context for decoding.</param>
        protected internal override void DecodeBody(ref SequenceReader<byte> reader, object context)
        {
            reader.TryReadLittleEndian(out long tarnsID);
            TransactionID = tarnsID;
        }
    }
}
