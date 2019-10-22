using System;
using System.Buffers;
using SuperSocket.ProtoBase;

namespace SciSharp.MySQL.Replication
{
    public class LogEventPipelineFilter : FixedHeaderPipelineFilter<LogEvent>
    {
        public LogEventPipelineFilter()
            : base(4)
        {
            Decoder = new LogEventPackageDecoder();
        }

        protected override int GetBodyLengthFromHeader(ReadOnlySequence<byte> buffer)
        {
            var pos = 0;
            var len = 0;

            foreach (var piece in buffer)
            {
                for (var i = 0; i < piece.Length && pos < 3; i++)
                {
                    len = len | ((0xff & piece.Span[i]) << (pos * 8));
                    pos++;
                }
            }

            return len;
        }
    }
}
