using System;
using System.Buffers;
using SuperSocket.ProtoBase;

namespace SciSharp.MySQL.Replication
{
    /// <summary>
    /// Pipeline filter that processes MySQL binary log event data from a network stream.
    /// This class is responsible for extracting complete log events from the stream.
    /// https://dev.mysql.com/doc/internals/en/binlog-event.html
    /// https://dev.mysql.com/doc/internals/en/binlog-event-header.html
    /// </summary>
    public class LogEventPipelineFilter : FixedHeaderPipelineFilter<LogEvent>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LogEventPipelineFilter"/> class.
        /// </summary>
        public LogEventPipelineFilter()
            : base(3)
        {
            Decoder = new LogEventPackageDecoder();
            Context = new ReplicationState();
        }

        /// <summary>
        /// Gets the body length of a MySQL packet from its header.
        /// </summary>
        /// <param name="buffer">The buffer containing the packet header.</param>
        /// <returns>The length of the packet body.</returns>
        protected override int GetBodyLengthFromHeader(ref ReadOnlySequence<byte> buffer)
        {
            var reader = new SequenceReader<byte>(buffer);            
            
            reader.TryRead(out byte byte0);
            reader.TryRead(out byte byte1);
            reader.TryRead(out byte byte2);

            return byte2 * 256 * 256 + byte1 * 256 + byte0 + 1;
        }
    }
}
