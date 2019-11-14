using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Buffers;

namespace SciSharp.MySQL.Replication
{
    class DateType : IMySQLDataType
    {
        public object ReadValue(ref SequenceReader<byte> reader, int meta)
        {
            // 11111000 00000000 00000000
            // 00000100 00000000 00000000
            var value = reader.ReadInteger(3);
            var day = value % 32;
            value >>= 5;
            int month = value % 16;
            int year = value >> 4;

            return new DateTime(year, month, day, 0, 0, 0);
        }
    }
}
