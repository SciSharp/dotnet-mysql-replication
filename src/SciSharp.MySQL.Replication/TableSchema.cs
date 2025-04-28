using System;
using System.Collections.Generic;
using System.Linq;

namespace SciSharp.MySQL.Replication
{
    /// <summary>
    /// Represents the schema of a MySQL database table.
    /// </summary>
    internal class TableSchema
    {
        /// <summary>
        /// Gets or sets the ID of the table.
        /// </summary>
        public long TableID { get; set; }

        /// <summary>
        /// Gets or sets the name of the table.
        /// </summary>
        public string TableName { get; set; }
        
        /// <summary>
        /// Gets or sets the database name containing the table.
        /// </summary>
        public string DatabaseName { get; set; }
        
        /// <summary>
        /// Gets or sets the collection of columns in the table.
        /// </summary>
        public IReadOnlyList<ColumnSchema> Columns { get; set; }
    }
    
    /// <summary>
    /// Represents information about a database column.
    /// </summary>
    internal class ColumnSchema
    {
        /// <summary>
        /// Gets or sets the name of the column.
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Gets or sets the MySQL data type of the column.
        /// </summary>
        public string DataType { get; set; }

        /// <summary>
        /// Gets or sets column size.
        /// </summary>
        public ulong ColumnSize { get; set; }
    }
}