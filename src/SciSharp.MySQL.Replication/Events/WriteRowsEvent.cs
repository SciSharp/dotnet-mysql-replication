using System;

namespace SciSharp.MySQL.Replication.Events
{
    /// <summary>
    /// Represents a MySQL binary log event that contains rows inserted into a table.
    /// This event is generated for an insert operation on a MySQL table.
    /// </summary>
    public sealed class WriteRowsEvent :  RowsEvent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WriteRowsEvent"/> class.
        /// </summary>
        public WriteRowsEvent()
            : base()
        {

        }
    }
}
