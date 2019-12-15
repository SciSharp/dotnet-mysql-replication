using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using SuperSocket.ProtoBase;

namespace SciSharp.MySQL.Replication
{
    public sealed class WriteRowsEvent :  RowsEvent
    {

        public WriteRowsEvent()
            : base()
        {

        }
    }
}
