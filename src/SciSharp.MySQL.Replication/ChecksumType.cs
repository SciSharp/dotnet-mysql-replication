using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace SciSharp.MySQL.Replication
{
    public enum ChecksumType : int
    {
        NONE = 0,
        CRC32 = 4
    }
}
