using System;

namespace SciSharp.MySQL.Replication.Events
{
    /// <summary>
    /// Specifies flags that provide additional information about rows events in MySQL replication.
    /// These flags describe configuration settings and state information for row operations.
    /// </summary>
    [Flags]
    public enum RowsEventFlags : byte
    {
        /// <summary>
        /// Indicates the end of a statement.
        /// </summary>
        EndOfStatement = 0x01,

        /// <summary>
        /// Indicates that foreign key checks are disabled.
        /// </summary>
        NoForeignKeyChecks = 0x02,
        
        /// <summary>
        /// Indicates that unique key checks are disabled.
        /// </summary>
        NoUniqueKeyChecks = 0x04,
        
        /// <summary>
        /// Indicates that row has a columns bitmap.
        /// </summary>
        RowHasAColumns = 0x08    
    }
}
