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
                Precision = (int)columnMetadata.MetadataValue[0],
                Scale = (int)columnMetadata.MetadataValue[1]
            };

            // Calculate storage size (MySQL packs 9 digits into 4 bytes)
            decimalOptions.IntegerBytes = CalculateByteCount(decimalOptions.Precision - decimalOptions.Scale);
            decimalOptions.FractionBytes = CalculateByteCount(decimalOptions.Scale);

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
            bool negative = (signByte & 0x80) == 0x00;
            signByte ^= 0x80;

            // Read integer part
            var intPart = ReadCompactDecimal(ref reader, options.IntegerBytes, negative, signByte);

            // Read integer part
            var fractionPart = ReadCompactDecimal(ref reader, options.FractionBytes, negative);

            // Convert to decimal using direct decimal operations
            decimal fraction = (decimal)fractionPart;

            if (options.Scale > 0)
            {
                // Create the appropriate scaling factor based on the scale
                decimal scaleFactor = (decimal)Math.Pow(10, options.Scale);
                fraction = fraction / scaleFactor;
            }

            var result = (decimal)intPart + fraction;

            // Apply sign
            return negative ? -result : result;
        }

        private static BigInteger ReadCompactDecimal(ref SequenceReader<byte> reader, int byteCount, bool flip, byte? signByteOverride = null)
        {
            if (byteCount == 0)
                return BigInteger.Zero;
            
            Span<byte> bytes = stackalloc byte[byteCount];
            reader.TryCopyTo(bytes);
            reader.Advance(byteCount);
            
            // Handle sign bit in the integer part
            if (signByteOverride.HasValue)
                bytes[0] = signByteOverride.Value;
            
            // Process each 4-byte group
            BigInteger result = BigInteger.Zero;

            for (int i = 0; i < byteCount; i += 4)
            {
                int groupSize = Math.Min(4, byteCount - i);
                int value = 0;
                
                // Combine bytes in group (big-endian within the group)
                for (int j = 0; j < groupSize; j++)
                {
                    var cellValue = bytes[i + j];

                    if (flip)
                        cellValue = (byte)(~cellValue);

                    value = (value << 8) | cellValue;
                }
                
                // Each group represents a specific number of decimal digits
                int digitCount = Math.Min(9, DIGITS_PER_INTEGER[groupSize]);
                result = result * BigInteger.Pow(10, digitCount) + value;
            }
            
            return result;
        }

        private int CalculateByteCount(int digits)
        {
            if (digits == 0)
                return 0;

            return digits / 9 * 4 + (digits % 9 / 2) + (digits % 2);
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
