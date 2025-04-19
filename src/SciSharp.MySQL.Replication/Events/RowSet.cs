using System;
using System.Collections.Generic;

namespace SciSharp.MySQL.Replication.Events
{
    /// <summary>
    /// Represents a set of rows affected by a row-based replication event.
    /// Contains both the column names and the row data.
    /// </summary>
    public sealed class RowSet
    {
        /// <summary>
        /// Gets or sets the list of column names.
        /// </summary>
        /// <remarks>
        /// May be null if column names are not available in the binlog.
        /// </remarks>
        public IReadOnlyList<string> ColumnNames { get; set; }

        /// <summary>
        /// Gets or sets the list of rows.
        /// Each row is represented as an array of cell values.
        /// </summary>
        /// <remarks>
        /// For UPDATE_ROWS_EVENT, the cell values are instances of CellValue containing both before and after values.
        /// For WRITE_ROWS_EVENT and DELETE_ROWS_EVENT, the cell values are the direct column values.
        /// </remarks>
        public IReadOnlyList<object[]> Rows { get; set; }

        /// <summary>
        /// Converts the rows into a readable format as a list of dictionaries.
        /// Each dictionary represents a row with column names as keys and cell values as values.
        /// </summary>
        /// <returns>A list of dictionaries representing the rows.</returns>
        /// <exception cref="Exception">Thrown when column names are not available.</exception>
        public IReadOnlyList<IDictionary<string, object>> ToReadableRows()
        {
            var columnNames = ColumnNames;

            if (columnNames == null || columnNames.Count == 0)
                throw new Exception("No column name is available.");

            var list = new List<IDictionary<string, object>>();

            foreach (var row in Rows)
            {
                var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

                for (var i = 0; i < row.Length; i++)
                {
                    var columnName = columnNames[i];
                    dict.Add(columnName, row[i]);
                }

                list.Add(dict);
            }

            return list;
        } 
    }
}
