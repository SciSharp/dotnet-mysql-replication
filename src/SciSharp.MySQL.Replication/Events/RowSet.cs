using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using SuperSocket.ProtoBase;

namespace SciSharp.MySQL.Replication
{
    public sealed class RowSet
    {
        public IReadOnlyList<string> ColumnNames { get; set; }

        public IReadOnlyList<object[]> Rows { get; set; }

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
