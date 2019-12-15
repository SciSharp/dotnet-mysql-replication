using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using SuperSocket.ProtoBase;

namespace SciSharp.MySQL.Replication
{
    [Flags]
    public enum RowsEventFlags : byte
    {
        EndOfStatement = 0x01,
        NoForeignKeyChecks = 0x02,
        NoUniqueKeyChecks = 0x04,
        RowHasAColumns = 0x08    
    }
}
