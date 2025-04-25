namespace SciSharp.MySQL.Replication.Types
{
    /// <summary>
    /// Interface for loading metadata values to column metadata.
    /// </summary>
    public interface IColumnMetadataLoader
    {
        /// <summary>
        /// Loads the metadata value for a given column metadata.
        /// </summary>
        /// <param name="columnMetadata">The column metadata.</param>
        void LoadMetadataValue(ColumnMetadata columnMetadata);
    }
}