using System;

namespace SciSharp.MySQL.Replication.Events
{
    /// <summary>
    /// Represents a value of a cell in a row, containing both old and new values.
    /// Used in replication events to track changes between states of a row.
    /// </summary>
    public class CellValue
    {
        /// <summary>
        /// Gets or sets the old value of the cell.
        /// </summary>
        public object OldValue { get; set; }

        /// <summary>
        /// Gets or sets the new value of the cell.
        /// </summary>
        public object NewValue { get; set; }
    }
}