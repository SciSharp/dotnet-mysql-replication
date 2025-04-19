using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using SuperSocket.ProtoBase;

namespace SciSharp.MySQL.Replication
{
    /// <summary>
    /// Extension methods for SequenceReader to help parse MySQL binary log formats.
    /// </summary>
    public static class SequenceReaderExtensions
    {
        /// <summary>
        /// Reads a BitArray from the binary stream.
        /// </summary>
        /// <param name="reader">The sequence reader.</param>
        /// <param name="length">The number of bits to read.</param>
        /// <param name="bigEndian">If true, interprets the bits in big-endian order.</param>
        /// <returns>The BitArray with the read bits.</returns>
        internal static BitArray ReadBitArray(ref this SequenceReader<byte> reader, int length, bool bigEndian = false)
        {
            var dataLen = (length + 7) / 8;
            var array = new BitArray(length, false);

            if (!bigEndian)
            {
                for (int i = 0; i < dataLen; i++)
                {
                    reader.TryRead(out byte b);
                    SetBitArray(array, b, i, length);
                }
            }
            else
            {
                for (int i = dataLen - 1; i >= 0; i--)
                {
                    reader.TryRead(out byte b);
                    SetBitArray(array, b, i, length);
                }
            }            

            return array;
        }

        private static void SetBitArray(BitArray array, byte b, int i, int length)
        {
            for (var j = i * 8; j < Math.Min((i + 1) * 8, length); j++)
            {
                if ((b & (0x01 << (j % 8))) != 0x00)
                    array.Set(j, true);
            }
        }

        /// <summary>
        /// Reads a string from the binary stream using the specified encoding.
        /// </summary>
        /// <param name="reader">The sequence reader.</param>
        /// <param name="encoding">The encoding to use.</param>
        /// <returns>The decoded string.</returns>
        internal static string ReadString(ref this SequenceReader<byte> reader, Encoding encoding)
        {
            return ReadString(ref reader, encoding, out long consumed);
        }

        /// <summary>
        /// Reads a string from the binary stream using the specified encoding and outputs the number of bytes consumed.
        /// </summary>
        /// <param name="reader">The sequence reader.</param>
        /// <param name="encoding">The encoding to use.</param>
        /// <param name="consumed">The number of bytes consumed.</param>
        /// <returns>The decoded string.</returns>
        internal static string ReadString(ref this SequenceReader<byte> reader, Encoding encoding, out long consumed)
        {
            if (encoding == null)
                encoding = Encoding.UTF8;

            if (reader.TryReadTo(out ReadOnlySequence<byte> seq, 0x00, false))
            {
                consumed = seq.Length + 1;
                var result = seq.GetString(encoding);
                reader.Advance(1);
                return result;
            }
            else
            {
                consumed = reader.Remaining;
                seq = reader.Sequence;
                seq = seq.Slice(reader.Consumed);
                var result = seq.GetString(Encoding.UTF8);
                reader.Advance(consumed);
                return result;
            }
        }

        /// <summary>
        /// Reads a string from the binary stream with a specified length.
        /// </summary>
        /// <param name="reader">The sequence reader.</param>
        /// <param name="length">The length of the string in bytes.</param>
        /// <returns>The decoded string.</returns>
        internal static string ReadString(ref this SequenceReader<byte> reader, long length = 0)
        {
            return ReadString(ref reader, Encoding.UTF8, length);
        }
        
        /// <summary>
        /// Reads a string from the binary stream using the specified encoding and length.
        /// </summary>
        /// <param name="reader">The sequence reader.</param>
        /// <param name="encoding">The encoding to use.</param>
        /// <param name="length">The length of the string in bytes.</param>
        /// <returns>The decoded string.</returns>
        internal static string ReadString(ref this SequenceReader<byte> reader, Encoding encoding, long length = 0)
        {
            if (length == 0 || reader.Remaining <= length)
                return ReadString(ref reader, encoding);

            // reader.Remaining > length
            var seq = reader.Sequence.Slice(reader.Consumed, length);            
            var consumed = 0L;
            
            try
            {
                var subReader = new SequenceReader<byte>(seq);
                return ReadString(ref subReader, encoding, out consumed);
            }
            finally
            {
                reader.Advance(length);
            }
        }

        /// <summary>
        /// Reads a fixed-length integer from the binary stream.
        /// </summary>
        /// <param name="reader">The sequence reader.</param>
        /// <param name="length">The number of bytes to read (1-4).</param>
        /// <returns>The decoded integer value.</returns>
        internal static int ReadInteger(ref this SequenceReader<byte> reader, int length)
        {
            if (length > 4)
                throw new ArgumentException("Length cannot be more than 4.", nameof(length));
    
            var unit = 1;
            var value = 0;

            for (var i = 0; i < length; i++)
            {
                reader.TryRead(out byte thisValue);
                value += thisValue * unit;
                unit *= 256;
            }

            return value;
        }

        /// <summary>
        /// Reads a big-endian integer from the binary stream.
        /// </summary>
        /// <param name="reader">The sequence reader.</param>
        /// <param name="length">The number of bytes to read (1-4).</param>
        /// <returns>The decoded integer value.</returns>
        internal static int ReadBigEndianInteger(ref this SequenceReader<byte> reader, int length)
        {
            if (length > 4)
                throw new ArgumentException("Length cannot be more than 4.", nameof(length));
    
            var unit = (int)Math.Pow(256, length - 1);
            var value = 0;

            for (var i = 0; i < length; i++)
            {
                reader.TryRead(out byte thisValue);
                value += thisValue * (int)Math.Pow(256, length - i - 1);
            }

            return value;
        }

        /// <summary>
        /// Reads a fixed-length long integer from the binary stream.
        /// </summary>
        /// <param name="reader">The sequence reader.</param>
        /// <param name="length">The number of bytes to read (1-8).</param>
        /// <returns>The decoded long integer value.</returns>
        internal static long ReadLong(ref this SequenceReader<byte> reader, int length)
        {
            var unit = 1;
            var value = 0L;

            for (var i = 0; i < length; i++)
            {
                reader.TryRead(out byte thisValue);
                value += thisValue * unit;
                unit *= 256;
            }

            return value;
        }

        /// <summary>
        /// Reads a length-encoded string from the binary stream.
        /// </summary>
        /// <param name="reader">The sequence reader.</param>
        /// <returns>The decoded string.</returns>
        internal static string ReadLengthEncodedString(ref this SequenceReader<byte> reader)
        {
            return ReadLengthEncodedString(ref reader, Encoding.UTF8);
        }

        /// <summary>
        /// Reads a length-encoded string from the binary stream using the specified encoding.
        /// </summary>
        /// <param name="reader">The sequence reader.</param>
        /// <param name="encoding">The encoding to use.</param>
        /// <returns>The decoded string.</returns>
        internal static string ReadLengthEncodedString(ref this SequenceReader<byte> reader, Encoding encoding)
        {
            var len = reader.ReadLengthEncodedInteger();

            if (len < 0)
                return null;

            if (len == 0)
                return string.Empty;

            return ReadString(ref reader, encoding, len);
        }

        /// <summary>
        /// Reads a length-encoded integer from the binary stream.
        /// </summary>
        /// <param name="reader">The sequence reader.</param>
        /// <returns>The decoded integer value.</returns>
        internal static long ReadLengthEncodedInteger(ref this SequenceReader<byte> reader)
        {
            reader.TryRead(out byte b0);

            if (b0 == 0xFB) // 251
                return -1;

            if (b0 == 0xFC) // 252
            {
                reader.TryReadLittleEndian(out short shortValue);
                return (long)shortValue;
            }

            if (b0 == 0xFD) // 253
            {
                reader.TryRead(out byte b1);
                reader.TryRead(out byte b2);
                reader.TryRead(out byte b3);

                return (long)(b1 + b2 * 256 + b3 * 256 * 256);
            }

            if (b0 == 0xFE) // 254
            {
                reader.TryReadLittleEndian(out long longValue);
                return longValue;
            }

            return (long)b0;
        }
    }
}
