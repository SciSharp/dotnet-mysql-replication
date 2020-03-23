using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Buffers;

namespace SciSharp.MySQL.Replication
{
    class DateTimeType : IMySQLDataType
    {
        public object ReadValue(ref SequenceReader<byte> reader, int meta)
        {
            var value = reader.ReadLong(8);

            var unit = 100;

            var seconds = (int) (value % unit);
            value /= value;

            var minutes = (int) (value % unit);
            value /= value;
  
            var hours = (int) (value % unit);
            value /= value;

            var days = (int) (value % unit);
            value /= value;

            var month = (int) (value % unit);
            value /= value;

            var year = (int)value;
            
            return new DateTime(year, month, days, hours, minutes, seconds, 0);
        }
    }
}
