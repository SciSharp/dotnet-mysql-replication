using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using SuperSocket.ProtoBase;

namespace SciSharp.MySQL.Replication
{
    public static class SequenceReaderExtensions
    {
        internal static BitArray ReadBitArray(ref this SequenceReader<byte> reader, int length, bool defaultValue = false)
        {
            var dataLen = (length + 7) / 8;
            var array = new BitArray(length, defaultValue);
    
            for (int i = 0; i < dataLen; i++)
            {
                reader.TryRead(out byte b);

                for (var j = i * 8; j < Math.Min((i + 1) * 8, length); j++)
                {
                    if ((b & (0x01 << (j % 8))) != 0x00)
                        array.Set(j, true);
                }
            }

            return array;
        }

        internal static string ReadString(ref this SequenceReader<byte> reader, Encoding encoding)
        {
            return ReadString(ref reader, encoding, out long consumed);
        }

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

        internal static string ReadString(ref this SequenceReader<byte> reader, long length = 0)
        {
            return ReadString(ref reader, Encoding.UTF8, length);
        }
        
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

        internal static string ReadLengthEncodedString(ref this SequenceReader<byte> reader)
        {
            return ReadLengthEncodedString(ref reader, Encoding.UTF8);
        }

        internal static string ReadLengthEncodedString(ref this SequenceReader<byte> reader, Encoding encoding)
        {
            var len = reader.ReadLengthEncodedInteger();

            if (len < 0)
                return null;

            if (len == 0)
                return string.Empty;

            return ReadString(ref reader, encoding, len);
        }

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
