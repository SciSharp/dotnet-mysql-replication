using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using SciSharp.MySQL.Replication.Types;

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

        /// <summary>
        /// Gets the list of column metadata.
        /// </summary>
        public IReadOnlyList<ColumnMetadata> Columns { get; private set; }

        /// <summary>
        /// Builds the list of column metadata based on the provided column types.
        /// This method is typically called after the table metadata has been fully populated.
        /// </summary>
        /// <param name="columnTypes">The column types.</param>
        /// <param name="columnMetadataValues">The metadata values for each column.</param>
        public void BuildColumnMetadataList(IReadOnlyList<ColumnType> columnTypes, IReadOnlyList<int> columnMetadataValues)
        {
            var columnMetadatas = new List<ColumnMetadata>(ColumnNames.Count);

            var numericColumnIndex = -1;

            for (int i = 0; i < columnTypes.Count; i++)
            {
                var columnType = columnTypes[i];
                var isNumberColumn = columnType.IsNumberColumn();
                numericColumnIndex++;

                var columnMetadata = new ColumnMetadata
                {
                    Name = ColumnNames[i],
                    Type = columnType,
                    CharsetId = ColumnCharsets != null ? ColumnCharsets[i] : 0,
                    //If the bit is set (1), the column is UNSIGNED; if not set (0), it's SIGNED (default)
                    IsUnsigned = isNumberColumn ? Signedness[numericColumnIndex] : false,
                    EnumValues = EnumStrValues != null ? EnumStrValues[i] : null,
                    SetValues = SetStrValues != null  ? SetStrValues[i] : null,
                    MetadataValue = (short)columnMetadataValues[i],
                    NumericColumnIndex = isNumberColumn ? numericColumnIndex : -1
                };
                
                columnMetadatas.Add(columnMetadata);
            }

            Columns = columnMetadatas;
        }
    }
}
