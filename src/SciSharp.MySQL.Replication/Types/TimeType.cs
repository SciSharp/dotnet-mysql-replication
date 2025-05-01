using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Buffers;

namespace SciSharp.MySQL.Replication.Types
{
    /// <summary>
    /// Represents the MySQL TIME data type.
    /// </summary>
    /// <remarks>
    /// Handles the reading and conversion of MySQL TIME values from binary log.
    /// </remarks>
    class TimeType : IMySQLDataType
    {
        /// <summary>
        /// Reads a TIME value from the binary log.
        /// </summary>
        /// <param name="reader">The sequence reader containing the bytes to read.</param>
        /// <param name="columnMetadata">Metadata for the column.</param>
        /// <returns>A TimeSpan representing the MySQL TIME value.</returns>
        public object ReadValue(ref SequenceReader<byte> reader, ColumnMetadata columnMetadata)
        {
            // In binary log format, TIME is encoded as a 3-byte integer
            int encodedValue = 0;
            
            reader.TryRead(out byte b0);
            reader.TryRead(out byte b1);
            reader.TryRead(out byte b2);
            
            // Combine the 3 bytes into an integer (little-endian)
            encodedValue = b0 | (b1 << 8) | (b2 << 16);
            
            // Check if negative (bit 23 is the sign bit)
            bool isNegative = (encodedValue & 0x800000) != 0;
            
            // Handle negative values
            if (isNegative)
            {
                // Clear the sign bit for calculations
                encodedValue &= 0x7FFFFF;
            }
            
            // Binary log format has time in a packed decimal format:
            // HHHHHH MMMMMM SSSSSS (each field taking 6 bits in binary log)
            // But we need to convert it to HHMMSS for TimeSpan
            
            int hours = encodedValue / 10000;
            int minutes = (encodedValue % 10000) / 100;
            int seconds = encodedValue % 100;
            
            // Validate time components
            if (hours > 838 || minutes > 59 || seconds > 59)
            {
                throw new InvalidOperationException($"Invalid TIME value: {encodedValue}");
            }
            
            // Create TimeSpan (MySQL TIME can represent larger ranges than .NET TimeSpan)
            TimeSpan result = new TimeSpan(hours, minutes, seconds);
            
            // Apply sign if needed
            return isNegative ? result.Negate() : result;
        }
    }
}
