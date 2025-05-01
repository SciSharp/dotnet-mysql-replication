using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Buffers;

namespace SciSharp.MySQL.Replication.Types
{
    /// <summary>
    /// Represents the MySQL TIME2 data type.
    /// </summary>
    /// <remarks>
    /// Handles the reading and conversion of MySQL TIME values with fractional seconds.
    /// </remarks>
    class TimeV2Type : IMySQLDataType, IColumnMetadataLoader
    {
        /// <summary>
        /// Loads the fractional seconds precision from the column metadata.
        /// </summary>
        /// <param name="columnMetadata">The column metadata containing the precision value.</param>
        public void LoadMetadataValue(ColumnMetadata columnMetadata)
        {
            // For TIME2 type, the metadata value represents the precision of fractional seconds
            // MySQL supports precision values from 0 to 6 (microsecond precision)
            columnMetadata.Options = new TimeV2Options
            {
                FractionalSecondsPrecision = columnMetadata.MetadataValue[0]
            };
        }

        /// <summary>
        /// Reads a TIME2 value from the binary log.
        /// </summary>
        /// <param name="reader">The sequence reader containing the bytes to read.</param>
        /// <param name="columnMetadata">Metadata for the column defining fractional second precision.</param>
        /// <returns>A TimeSpan representing the MySQL TIME2 value.</returns>
        public object ReadValue(ref SequenceReader<byte> reader, ColumnMetadata columnMetadata)
        {
            // Get the fractional seconds precision from metadata (0-6)
            int fsp = columnMetadata.Options is TimeV2Options options ? options.FractionalSecondsPrecision : 0;

            // Read the integer part (3 bytes)
            byte intPartByte1, intPartByte2, intPartByte3;
            reader.TryRead(out intPartByte1);
            reader.TryRead(out intPartByte2);
            reader.TryRead(out intPartByte3);

            // Combine into a 24-bit integer, bigendian
            int intPart = (intPartByte1 << 16) | (intPartByte2 << 8) | intPartByte3;

            // In MySQL 5.6.4+ TIME2 format:
            // Bit 1 (MSB): Sign bit (1=negative, 0=positive)
            // Bits 2-24: Packed BCD encoding of time value
            bool isNegative = ((intPart & 0x800000) == 0); // In MySQL TIME2, 0 is negative, 1 is positive
            
            // If negative, apply two's complement
            if (isNegative)
            {
                intPart = ~intPart + 1;
                intPart &= 0x7FFFFF; // Keep only the 23 bits for the absolute value
            }

            // TIME2 is packed in a special format:
            // Bits 2-13: Hours (12 bits)
            // Bits 14-19: Minutes (6 bits)
            // Bits 20-25: Seconds (6 bits)
            int hours = (intPart >> 12) & 0x3FF;
            int minutes = (intPart >> 6) & 0x3F;
            int seconds = intPart & 0x3F;

            // Read fractional seconds if precision > 0
            int microseconds = 0;
            if (fsp > 0)
            {
                // Calculate bytes needed for the requested precision
                int fractionalBytes = (fsp + 1) / 2;
                int fraction = 0;

                // Read bytes for fractional seconds
                for (int i = 0; i < fractionalBytes; i++)
                {
                    byte b;
                    reader.TryRead(out b);
                    fraction = (fraction << 8) | b;
                }

                // Convert to microseconds based on precision
                int scaleFactor = 1000000;
                switch (fsp)
                {
                    case 1: scaleFactor = 100000; break;
                    case 2: scaleFactor = 10000; break;
                    case 3: scaleFactor = 1000; break;
                    case 4: scaleFactor = 100; break;
                    case 5: scaleFactor = 10; break;
                    case 6: scaleFactor = 1; break;
                }
                
                microseconds = fraction * scaleFactor;
            }

            // Create TimeSpan
            TimeSpan result;
            
            if (hours >= 24)
            {
                // For large hour values, convert to days + remaining hours
                int days = hours / 24;
                int remainingHours = hours % 24;
                
                // Create TimeSpan with days, hours, minutes, seconds, ms
                result = new TimeSpan(days, remainingHours, minutes, seconds, microseconds / 1000);
                
                // Add remaining microseconds as ticks (1 tick = 100 nanoseconds, 1 microsecond = 10 ticks)
                if (microseconds % 1000 > 0)
                {
                    result = result.Add(TimeSpan.FromTicks((microseconds % 1000) * 10));
                }
            }
            else
            {
                // Standard case for hours < 24
                result = new TimeSpan(0, hours, minutes, seconds, microseconds / 1000);
                
                // Add microsecond precision as ticks
                if (microseconds % 1000 > 0)
                {
                    result = result.Add(TimeSpan.FromTicks((microseconds % 1000) * 10));
                }
            }

            // Apply sign
            return isNegative ? result.Negate() : result;
        }
    }

    class TimeV2Options
    {
        public int FractionalSecondsPrecision { get; set; } = 0;
    }
}
