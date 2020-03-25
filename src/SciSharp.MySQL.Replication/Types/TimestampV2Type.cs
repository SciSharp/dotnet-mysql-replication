using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Buffers;

namespace SciSharp.MySQL.Replication
{
    class TimestampV2Type : IMySQLDataType
    {
        public object ReadValue(ref SequenceReader<byte> reader, int meta)
        {
            var millis = (long)reader.ReadBigEndianInteger(4);
            var fsp = ReadFractionalSeconds(ref reader, meta);
            var ticks = millis * 1000 + fsp / 1000;
            return new DateTime(ticks);
        }

        private int ReadFractionalSeconds(ref SequenceReader<byte> reader, int meta)
        {
            int length = (meta + 1) / 2;

            if (length <= 0)
                return 0;

            int fraction = reader.ReadBigEndianInteger(length);
            return fraction * (int) Math.Pow(100, 3 - length);
        }
    }
}
