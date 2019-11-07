using System;
using System.Buffers;

namespace SciSharp.MySQL.Replication
{
    public sealed class EmptyPayloadEvent : LogEvent
    {
        protected internal override void DecodeBody(ref SequenceReader<byte> reader, object context)
        {
            
        }
    }
}
