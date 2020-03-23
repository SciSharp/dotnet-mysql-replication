using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Buffers;

namespace SciSharp.MySQL.Replication
{
    class DateTimeV2Type : IMySQLDataType
    {
        public object ReadValue(ref SequenceReader<byte> reader, int meta)
        {
            /*
            (in big endian)
            1 bit sign (1= non-negative, 0= negative)
            17 bits year*13+month (year 0-9999, month 0-12)
            5 bits day (0-31)
            5 bits hour (0-23)
            6 bits minute (0-59)
            6 bits second (0-59)
            (5 bytes in total)
            + fractional-seconds storage (size depends on meta)
            */

            reader.TryReadBigEndian(out int totalValue0);
            reader.TryRead(out byte totalValue1);

            var totalValue = (long)totalValue0 * 256 + totalValue1;

            if (totalValue == 0)
                return null;

            var seconds = (int)(totalValue & 0x3F);

            totalValue = totalValue >> 6;
            var minutes = (int)(totalValue & 0x3F);

            totalValue = totalValue >> 6;   
            var hours = (int)(totalValue & 0x1F);

            totalValue = totalValue >> 5;
            var days = (int)(totalValue & 0x1F);

            totalValue = totalValue >> 5;
            var yearMonths = (int)(totalValue & 0x01FFFF);

            var year = yearMonths / 13;
            var month = yearMonths % 13;
            var fsp = ReadFractionalSeconds(ref reader, meta);

            return new DateTime(year, month, days, hours, minutes, seconds, fsp / 1000);     
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
