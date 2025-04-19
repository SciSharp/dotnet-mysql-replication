using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections;
using System.Text;

namespace SciSharp.MySQL.Replication
{
    /// <summary>
    /// Contains metadata information about a MySQL table structure.
    /// </summary>
    public class TableMetadata
    {
        /// <summary>
        /// Gets or sets the signedness information for numeric columns.
        /// Each bit corresponds to a column, indicating whether it's signed (false) or unsigned (true).
        /// </summary>
        public BitArray Signedness { get; set; }
        
        /// <summary>
        /// Gets or sets the default charset information for the table.
        /// </summary>
        public DefaultCharset DefaultCharset { get; set; }
        
        /// <summary>
        /// Gets or sets the list of charset IDs for each column in the table.
        /// </summary>
        public List<int> ColumnCharsets { get; set; }
        
        /// <summary>
        /// Gets or sets the list of column names in the table.
        /// </summary>
        public List<string> ColumnNames { get; set; }
        
        /// <summary>
        /// Gets or sets the possible string values for SET columns.
        /// </summary>
        public List<string[]> SetStrValues { get; set; }
        
        /// <summary>
        /// Gets or sets the possible string values for ENUM columns.
        /// </summary>
        public List<string[]> EnumStrValues { get; set; }
        
        /// <summary>
        /// Gets or sets the types of GEOMETRY columns.
        /// </summary>
        public List<int> GeometryTypes { get; set; }
        
        /// <summary>
        /// Gets or sets the indexes of columns that are part of the primary key without a prefix.
        /// </summary>
        public List<int> SimplePrimaryKeys { get; set; }
        
        /// <summary>
        /// Gets or sets a dictionary mapping column indexes to their prefix length for primary key columns with a prefix.
        /// </summary>
        public Dictionary<int, int> PrimaryKeysWithPrefix { get; set; }
        
        /// <summary>
        /// Gets or sets the default charset information for ENUM and SET columns.
        /// </summary>
        public DefaultCharset EnumAndSetDefaultCharset { get; set; }
        
        /// <summary>
        /// Gets or sets the list of charset IDs for ENUM and SET columns.
        /// </summary>
        public List<int> EnumAndSetColumnCharsets { get; set; }
        
        /// <summary>
        /// Gets or sets the visibility information for columns.
        /// Each bit corresponds to a column, indicating whether it's visible (true) or invisible (false).
        /// </summary>
        public BitArray ColumnVisibility { get; set; }
    }
}
