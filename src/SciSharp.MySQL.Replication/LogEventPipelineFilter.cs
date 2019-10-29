using System;
using System.Buffers;
using SuperSocket.ProtoBase;

namespace SciSharp.MySQL.Replication
{
    /// <summary>
    /// https://dev.mysql.com/doc/internals/en/binlog-event.html
    /// https://dev.mysql.com/doc/internals/en/binlog-event-header.html
    /// </summary>
    public class LogEventPipelineFilter : FixedHeaderPipelineFilter<LogEvent>
    {
        public LogEventPipelineFilter()
            : base(3)
        {
            Decoder = new LogEventPackageDecoder();
        }

        protected override int GetBodyLengthFromHeader(ReadOnlySequence<byte> buffer)
        {
            var reader = new SequenceReader<byte>(buffer);            
            
            reader.TryRead(out byte byte0);
            reader.TryRead(out byte byte1);
            reader.TryRead(out byte byte2);

            var len = byte2 * 256 * 256 + byte1 * 256 + byte0 + 4 + 1;
            return len;
        }
    }
}
