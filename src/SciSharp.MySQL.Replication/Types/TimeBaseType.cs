using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Buffers;

namespace SciSharp.MySQL.Replication
{
    abstract class TimeBaseType
    {
        protected int ReadFractionalSeconds(ref SequenceReader<byte> reader, int meta)
        {
            int length = (meta + 1) / 2;

            if (length <= 0)
                return 0;

            int fraction = reader.ReadBigEndianInteger(length);
            return fraction * (int) Math.Pow(100, 3 - length);
        }
    }
}
