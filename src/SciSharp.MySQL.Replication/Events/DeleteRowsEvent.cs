using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using SuperSocket.ProtoBase;

namespace SciSharp.MySQL.Replication
{
    /// <summary>
    /// Represents a MySQL binary log event that contains rows deleted from a table.
    /// This event is generated for a delete operation on a row in a MySQL table.
    /// </summary>
    public sealed class DeleteRowsEvent :  RowsEvent
    {
        public DeleteRowsEvent()
            : base()
        {

        }
    }
}
