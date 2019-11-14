using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Buffers;

namespace SciSharp.MySQL.Replication
{
    class BitType : IMySQLDataType
    {
        public object ReadValue(ref SequenceReader<byte> reader, int meta)
        {
            int bitArrayLength = (meta >> 8) * 8 + (meta & 0xFF);
            return reader.ReadBitArray(bitArrayLength, false);
        }
    }
}
