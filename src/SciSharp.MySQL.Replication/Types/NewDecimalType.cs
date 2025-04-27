using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Buffers;
using System.Globalization;
using System.Linq.Expressions;
using System.Buffers.Binary;
using System.Numerics;

namespace SciSharp.MySQL.Replication.Types
{
    /// <summary>
    /// Represents the MySQL DECIMAL data type.
    /// </summary>
    /// <remarks>
    /// Handles the reading and conversion of MySQL DECIMAL values.
    /// </remarks>
    class NewDecimalType : IMySQLDataType, IColumnMetadataLoader
    {
        private static readonly IReadOnlyList<int> DIGITS_PER_INTEGER = new [] { 0, 9, 19, 28, 38 };

        /// <summary>
        /// Loads metadata for the DECIMAL column type.
        /// </summary>
        /// <param name="columnMetadata">the colummn metadata.</param>
        public void LoadMetadataValue(ColumnMetadata columnMetadata) 
        {
            var decimalOptions = new DecimalOptions
            {
                Precision = columnMetadata.MetadataValue & 0xFF,
                Scale = columnMetadata.MetadataValue >> 8
            };

            // Calculate storage size (MySQL packs 9 digits into 4 bytes)
            decimalOptions.IntegerBytes = (decimalOptions.Precision - decimalOptions.Scale + 8) / 9 * 4;
            decimalOptions.FractionBytes = (decimalOptions.Scale + 8) / 9 * 4;

            columnMetadata.Options = decimalOptions;
        }

        /// <summary>
        /// Reads a DECIMAL value from the binary log.
        /// </summary>
        /// <param name="reader">The sequence reader containing the bytes to read.</param>
        /// <param name="columnMetadata">Metadata for the column.</param>
        /// <returns>A decimal value representing the MySQL DECIMAL value.</returns>
        public object ReadValue(ref SequenceReader<byte> reader, ColumnMetadata columnMetadata)
        {
            var options = columnMetadata.Options as DecimalOptions;

            reader.TryPeek(out byte signByte);
            bool negative = (signByte & 0x80) == 0x80;

            // Read integer part
            var intPart = ReadCompactDecimal(ref reader, (int)Math.Min(options.IntegerBytes, reader.Remaining), true);
            
            // Read fraction part
            var fracPart = ReadCompactDecimal(ref reader, (int)Math.Min(options.FractionBytes, reader.Remaining), false);

            // Convert to decimal using direct decimal operations
            decimal intDecimal = (decimal)intPart;
            
            // Calculate the fractional part as decimal
            decimal fracDecimal = 0;

            if (fracPart > 0)
            {
                // Create the appropriate scaling factor based on the scale
                decimal scaleFactor = (decimal)Math.Pow(10, options.Scale);
                fracDecimal = (decimal)fracPart / scaleFactor;
            }

            var result = intDecimal + fracDecimal;

            // Apply sign
            return negative ? -result : result;
        }

        private static BigInteger ReadCompactDecimal(ref SequenceReader<byte> reader, int byteCount, bool isIntegerPart)
        {
            if (byteCount == 0)
                return BigInteger.Zero;
            
            Span<byte> bytes = stackalloc byte[byteCount];
            reader.TryCopyTo(bytes);
            reader.Advance(byteCount);
            
            // Handle sign bit in the integer part
            if (isIntegerPart)
                bytes[0] &= 0x7F;  // Clear the sign bit
            
            // Process each 4-byte group
            BigInteger result = BigInteger.Zero;

            for (int i = 0; i < byteCount; i += 4)
            {
                int groupSize = Math.Min(4, byteCount - i);
                int value = 0;
                
                // Combine bytes in group (big-endian within the group)
                for (int j = 0; j < groupSize; j++)
                {
                    value = (value << 8) | bytes[i + j];
                }
                
                // Each group represents a specific number of decimal digits
                int digitCount = Math.Min(9, DIGITS_PER_INTEGER[groupSize]);
                result = result * BigInteger.Pow(10, digitCount) + value;
            }
            
            return result;
        }

        class DecimalOptions
        {
            public int Precision { get; set; }
            public int Scale { get; set; }
            public int IntegerBytes { get; set; }
            public int FractionBytes { get; set; }
        }
    }
}
