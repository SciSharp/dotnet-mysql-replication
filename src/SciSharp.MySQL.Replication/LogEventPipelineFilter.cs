using System;
using System.Buffers;
using SuperSocket.ProtoBase;

namespace SciSharp.MySQL.Replication
{
    public class LogEventPipelineFilter : IPipelineFilter<LogEvent>
    {
        public IPackageDecoder<LogEvent> Decoder { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public IPipelineFilter<LogEvent> NextFilter => throw new NotImplementedException();

        public LogEvent Filter(ref SequenceReader<byte> reader)
        {
            throw new NotImplementedException();
        }

        public void Reset()
        {
            throw new NotImplementedException();
        }
    }
}
