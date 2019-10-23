using System;
using System.Buffers;
using SuperSocket.ProtoBase;

namespace SciSharp.MySQL.Replication
{
    /*
    https://dev.mysql.com/doc/internals/en/binlog-event.html
    https://dev.mysql.com/doc/internals/en/binlog-event-header.html
    */
    public class LogEventPipelineFilter : FixedHeaderPipelineFilter<LogEvent>
    {
        public LogEventPipelineFilter()
            : base(19)
        {
            Decoder = new LogEventPackageDecoder();
        }

        protected override int GetBodyLengthFromHeader(ReadOnlySequence<byte> buffer)
        {
            var reader = new SequenceReader<byte>(buffer);            
            reader.Advance(9);
            
            if (!reader.TryReadBigEndian(out int len))
                throw new Exception("No enought data to read");

            return len;
        }
    }
}
