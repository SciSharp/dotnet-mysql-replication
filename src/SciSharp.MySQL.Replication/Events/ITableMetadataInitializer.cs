namespace SciSharp.MySQL.Replication.Events
{
    interface ITableMetadataInitializer
    {
        void InitializeMetadata(TableMetadata tableMetadata);
    }
}