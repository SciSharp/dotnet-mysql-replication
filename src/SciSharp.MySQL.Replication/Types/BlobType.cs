using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Buffers;

namespace SciSharp.MySQL.Replication.Types
{
    /// <summary>
    /// Options specific to BLOB data type.
    /// </summary>
    class BlobOptions
    {
        /// <summary>
        /// Gets or sets the number of bytes used to store the BLOB length (1, 2, 3, or 4).
        /// </summary>
        public int LengthBytes { get; set; }
    }

    /// <summary>
    /// Represents the MySQL BLOB data type.
    /// </summary>
    /// <remarks>
    /// Handles the reading and conversion of MySQL BLOB values.
    /// </remarks>
    class BlobType : IMySQLDataType, IColumnMetadataLoader
    {
        /// <summary>
        /// Loads metadata for BLOB type.
        /// </summary>
        /// <param name="columnMetadata">The column metadata object.</param>
        public void LoadMetadataValue(ColumnMetadata columnMetadata)
        {
            // The metadata value for BLOB is the length of the size field in bytes (1, 2, 3, or 4)
            columnMetadata.Options = new BlobOptions
            {
                LengthBytes = columnMetadata.MetadataValue[0]
            };
        }
        
        /// <summary>
        /// Reads a BLOB value from the binary log.
        /// </summary>
        /// <param name="reader">The sequence reader containing the bytes to read.</param>
        /// <param name="columnMetadata">Metadata for the column.</param>
        /// <returns>A byte array representing the MySQL BLOB value.</returns>
        public object ReadValue(ref SequenceReader<byte> reader, ColumnMetadata columnMetadata)
        {
            // Get metadata length (bytes used for size field: 1, 2, 3, or 4) from Options
            int lengthBytes = columnMetadata.Options is BlobOptions options ? options.LengthBytes : 0;
            
            // Read the length of the BLOB based on metadata
            int blobLength = reader.ReadInteger(lengthBytes);
            
            // Validate blob length to prevent out-of-memory issues
            if (blobLength < 0 || blobLength > 1_000_000_000) // 1GB limit as safety check
            {
                throw new InvalidOperationException($"Invalid BLOB length: {blobLength}");
            }
            
            // Handle empty BLOB
            if (blobLength == 0)
            {
                return Array.Empty<byte>();
            }
            
            // Read the BLOB data
            byte[] blobData = new byte[blobLength];

            if (!reader.TryCopyTo(blobData.AsSpan()))
            {
                throw new InvalidOperationException($"Failed to read complete BLOB data (expected {blobLength} bytes)");
            }

            reader.Advance(blobLength);
            return blobData;
        }
    }
}
