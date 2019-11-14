using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Buffers;
using System.Globalization;

namespace SciSharp.MySQL.Replication
{
    class NewDecimalType : IMySQLDataType
    {
        public object ReadValue(ref SequenceReader<byte> reader, int meta)
        {
            return decimal.Parse(reader.ReadLengthEncodedString(), CultureInfo.InvariantCulture);
        }
    }
}
