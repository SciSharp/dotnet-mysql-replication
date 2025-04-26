using System;
using SciSharp.MySQL.Replication.Types;

namespace SciSharp.MySQL.Replication.Events
{
    class SetTypeTableMetadataInitializer : ITableMetadataInitializer
    {
        public void InitializeMetadata(TableMetadata metadata)
        {
            var setColumnIndex = 0;

            foreach (var column in metadata.Columns)
            {
                if (column.Type != ColumnType.SET)
                    continue;

                column.SetValues = metadata.SetStrValues[setColumnIndex];
                setColumnIndex++;
            }
        }
    }
}