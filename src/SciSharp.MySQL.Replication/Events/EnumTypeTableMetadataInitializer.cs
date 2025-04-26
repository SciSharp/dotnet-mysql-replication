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
            
            var meta0 = columnMetadata.MetadataValue >> 8;
            
            if (meta0 != (int)ColumnType.ENUM)
                return false;

            columnMetadata.UnderlyingType = ColumnType.ENUM;
            return true;
        }
    }
}