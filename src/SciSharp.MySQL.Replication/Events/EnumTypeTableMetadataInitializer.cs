using System;
using SciSharp.MySQL.Replication.Types;

namespace SciSharp.MySQL.Replication.Events
{
    class EnumTypeTableMetadataInitializer : ITableMetadataInitializer
    {
        public void InitializeMetadata(TableMetadata metadata)
        {
            var enumColumnIndex = 0;

            foreach (var column in metadata.Columns)
            {
                if (!IsEnumColumn(column))
                    continue;

                column.EnumValues = metadata.EnumStrValues[enumColumnIndex++];
            }
        }

        private bool IsEnumColumn(ColumnMetadata columnMetadata)
        {
            if (columnMetadata.Type == ColumnType.ENUM)
                return true;

            if (columnMetadata.Type != ColumnType.STRING)
                return false;
            
            // Len = 1 or 2
            return (columnMetadata.MetadataValue & 0xFF) < 3;
        }
    }
}