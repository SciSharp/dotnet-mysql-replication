using System;
using System.Buffers;
using SuperSocket.ProtoBase;

namespace SciSharp.MySQL.Replication
{
    class LogEventPackageDecoder : IPackageDecoder<LogEvent>
    {
        public LogEvent Decode(ReadOnlySequence<byte> buffer)
        {
            throw new NotImplementedException();
        }
    }
}
