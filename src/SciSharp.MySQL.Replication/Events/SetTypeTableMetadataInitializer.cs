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
                if (!IsSetColumn(column))
                    continue;

                column.SetValues = metadata.SetStrValues[setColumnIndex];
                setColumnIndex++;
            }
        }

        private bool IsSetColumn(ColumnMetadata columnMetadata)
        {
            if (columnMetadata.Type == ColumnType.SET)
                return true;

            if (columnMetadata.Type != ColumnType.STRING)
                return false;
            
            var meta0 = columnMetadata.MetadataValue >> 8;
            
            if (meta0 != (int)ColumnType.SET)
                return false;

            columnMetadata.UnderlyingType = ColumnType.SET;
            return true;
        }
    }
}