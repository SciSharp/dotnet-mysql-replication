using System.Collections.Generic;

namespace SciSharp.MySQL.Replication
{
    /// <summary>
        /// Represents metadata information for a database column.
        /// </summary>
        public class ColumnMetadata
        {
            /// <summary>
            /// Gets or sets the name of the column.
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// Gets or sets the column type.
            /// </summary>
            public ColumnType Type { get; set; }

            /// <summary>
            /// Gets or sets the metadata value associated with the column.
            /// </summary>
            public short MetadataValue { get; set; }

            /// <summary>
            /// Gets or sets the column max value length.
            /// </summary>
            public int MaxLength { get; set; }

            /// <summary>
            /// Gets or sets the character set ID for the column.
            /// </summary>
            public int CharsetId { get; set; }

            /// <summary>
            /// Gets or sets the set of possible values for an ENUM column type.
            /// </summary>
            public IReadOnlyList<string> EnumValues { get; set; }

            /// <summary>
            /// Gets or sets the set of possible values for a SET column type.
            /// </summary>
            public IReadOnlyList<string> SetValues { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether the column is visible.
            /// </summary>
            public bool IsVisible { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether the column is unsigned.
            /// </summary>
            public bool IsUnsigned { get; set; }

            /// <summary>
            /// Gets or sets the column index in all the numerci columns.
            /// </summary>
            public int NumericColumnIndex { get; set; }
        }
}