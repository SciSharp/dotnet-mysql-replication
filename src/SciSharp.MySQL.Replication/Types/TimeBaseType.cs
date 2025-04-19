using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Buffers;

namespace SciSharp.MySQL.Replication
{
    /// <summary>
    /// Base class for time-related MySQL data types.
    /// Provides common functionality for handling fractional seconds.
    /// </summary>
    abstract class TimeBaseType
    {
        /// <summary>
        /// Reads fractional seconds from the binary representation.
        /// </summary>
        /// <param name="reader">The sequence reader containing the binary data.</param>
        /// <param name="meta">The metadata defining the length of the fractional seconds part.</param>
        /// <returns>The fractional seconds value.</returns>
        protected int ReadFractionalSeconds(ref SequenceReader<byte> reader, int meta)
        {
            int length = (meta + 1) / 2;

            if (length <= 0)
                return 0;

            int fraction = reader.ReadBigEndianInteger(length);
            return fraction * (int) Math.Pow(100, 3 - length);
        }
    }
}
