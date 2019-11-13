using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Buffers;

namespace SciSharp.MySQL.Replication
{
    internal interface IMySQLDataType
    {
        object ReadValue(ref SequenceReader<byte> reader, int meta);
    }
}
