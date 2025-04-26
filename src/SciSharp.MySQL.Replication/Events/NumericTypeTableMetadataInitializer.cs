using System;
using SciSharp.MySQL.Replication.Types;

namespace SciSharp.MySQL.Replication.Events
{
    class NumericTypeTableMetadataInitializer : ITableMetadataInitializer
    {
        public void InitializeMetadata(TableMetadata metadata)
        {
            var numericColumnIndex = 0;

            foreach (var column in metadata.Columns)
            {
                if (!column.Type.IsNumberColumn())
                    continue;

                column.IsUnsigned = metadata.Signedness[numericColumnIndex];
                column.NumericColumnIndex = numericColumnIndex;

                numericColumnIndex++;
            }
        }
    }
}