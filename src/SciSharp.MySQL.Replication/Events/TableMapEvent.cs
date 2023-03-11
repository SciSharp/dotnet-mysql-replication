using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SuperSocket.ProtoBase;

namespace SciSharp.MySQL.Replication
{
    /// <summary>
    /// https://dev.mysql.com/doc/internals/en/table-map-event.html
    /// </summary>
    public sealed class TableMapEvent : LogEvent
    {
        public long TableID { get; set; }

        public string SchemaName { get; set; }

        public string TableName { get; set; }

        public int ColumnCount { get; set; }

        public byte[] ColumnTypes { get; set; }

        public int[] ColumnMetadata { get; set; }        
        
        public BitArray NullBitmap { get; set; }

        public TableMetadata Metadata { get; set; }
        
        public TableMapEvent()
        {
            HasCRC = true;
        }

        protected internal override void DecodeBody(ref SequenceReader<byte> reader, object context)
        {
            TableID = reader.ReadLong(6);

            reader.Advance(2); // skip flags

            byte len;
            reader.TryRead(out len);
            SchemaName = reader.ReadString(len);

            reader.TryRead(out len);// 0x00

            reader.TryRead(out len);
            TableName = reader.ReadString(len);

            reader.TryRead(out len);// 0x00

            ColumnCount = (int)reader.ReadLengthEncodedInteger();
            ColumnTypes = reader.Sequence.Slice(reader.Consumed, ColumnCount).ToArray();
            reader.Advance(ColumnCount);

            reader.ReadLengthEncodedInteger();

            ColumnMetadata = ReadColumnMetadata(ref reader, ColumnTypes);
            
            NullBitmap = reader.ReadBitArray(ColumnCount);

            RebuildReaderAsCRC(ref reader);

            Metadata = ReadTableMetadata(ref reader);

            if (context is ReplicationState repState)
            {
                repState.TableMap[TableID] = this;
            }
        }

        public override string ToString()
        {
            return $"{EventType.ToString()}\r\nTableID: {TableID}\r\nSchemaName: {SchemaName}\r\nTableName: {TableName}\r\nColumnCount: {ColumnCount}";
        }

        private int[] ReadColumnMetadata(ref SequenceReader<byte> reader, byte[] columnTypes)
        {
            var columnMetadata = new int[columnTypes.Length];

            for (int i = 0; i < columnTypes.Length; i++)
            {
                switch((ColumnType)columnTypes[i])
                {
                    case ColumnType.FLOAT:
                    case ColumnType.DOUBLE:
                    case ColumnType.BLOB:
                    case ColumnType.JSON:
                    case ColumnType.GEOMETRY:
                        columnMetadata[i] = (int)reader.ReadLong(1);
                        break;
                    case ColumnType.BIT:
                    case ColumnType.VARCHAR:
                    case ColumnType.NEWDECIMAL:
                        columnMetadata[i] = (int)reader.ReadLong(2);
                        break;
                    case ColumnType.SET:
                    case ColumnType.ENUM:
                    case ColumnType.STRING:
                        reader.TryReadBigEndian(out short value);
                        columnMetadata[i] = (int)value;
                        break;
                    case ColumnType.TIME_V2:
                    case ColumnType.DATETIME_V2:
                    case ColumnType.TIMESTAMP_V2:
                        columnMetadata[i] = (int)reader.ReadLong(1);
                        break;
                    default:
                        columnMetadata[i] = 0;
                        break;
                }
            }

            return columnMetadata;
        }

        private bool IsNumberColumn(ColumnType columnType)
        {
            switch (columnType)
            {
                case ColumnType.TINY:
                case ColumnType.SHORT:
                case ColumnType.INT24:
                case ColumnType.LONG:
                case ColumnType.LONGLONG:
                case ColumnType.NEWDECIMAL:
                case ColumnType.FLOAT:
                case ColumnType.DOUBLE:
                    return true;
                default:
                    return false;
            }
        }

        private TableMetadata ReadTableMetadata(ref SequenceReader<byte> reader)
        {
            var numericColumnCount = ColumnTypes.Count(c => IsNumberColumn((ColumnType)c));

            var metadata = new TableMetadata();

            while (reader.Remaining > 0)
            {
                reader.TryRead(out byte filedTypeCode);

                var fieldType = (MetadataFieldType)filedTypeCode;
                var length = reader.ReadLengthEncodedInteger();        

                var subReader = new SequenceReader<byte>(reader.Sequence.Slice(reader.Consumed, length));
                
                try
                {
                    ReadMetadataField(ref subReader, fieldType, metadata, numericColumnCount);
                }
                finally
                {
                    reader.Advance(length);
                }
            }

            return metadata;
        }

        private void ReadMetadataField(ref SequenceReader<byte> subReader, MetadataFieldType fieldType, TableMetadata metadata, int numericColumnCount)
        {
            switch (fieldType)
            {
                case MetadataFieldType.SIGNEDNESS:
                    metadata.Signedness = subReader.ReadBitArray(numericColumnCount);
                    break;

                case MetadataFieldType.DEFAULT_CHARSET:
                    metadata.DefaultCharset = ReadDefaultCharset(ref subReader);
                    break;

                case MetadataFieldType.COLUMN_CHARSET:
                    metadata.ColumnCharsets = ReadIntegers(ref subReader);
                    break;

                case MetadataFieldType.COLUMN_NAME:
                    metadata.ColumnNames = ReadStringList(ref subReader);
                    break;

                case MetadataFieldType.SET_STR_VALUE:
                    metadata.SetStrValues = ReadTypeValues(ref subReader);
                    break;

                case MetadataFieldType.ENUM_STR_VALUE:
                    metadata.EnumStrValues = ReadTypeValues(ref subReader);
                    break;

                case MetadataFieldType.GEOMETRY_TYPE:
                    metadata.GeometryTypes = ReadIntegers(ref subReader);
                    break;

                case MetadataFieldType.SIMPLE_PRIMARY_KEY:
                    metadata.SimplePrimaryKeys = ReadIntegers(ref subReader);
                    break;

                case MetadataFieldType.PRIMARY_KEY_WITH_PREFIX:
                    metadata.PrimaryKeysWithPrefix = ReadIntegerDictionary(ref subReader);
                    break;

                case MetadataFieldType.ENUM_AND_SET_DEFAULT_CHARSET:
                    metadata.EnumAndSetDefaultCharset = ReadDefaultCharset(ref subReader);
                    break;

                case MetadataFieldType.ENUM_AND_SET_COLUMN_CHARSET:
                    metadata.EnumAndSetColumnCharsets = ReadIntegers(ref subReader);
                    break;
                
                case MetadataFieldType.COLUMN_VISIBILITY:
                    metadata.ColumnVisibility = subReader.ReadBitArray(ColumnCount);
                    break;

                default:
                    throw new Exception("Unsupported table metadata field type: " + fieldType);
            }
        }

        private List<string[]> ReadTypeValues(ref SequenceReader<byte> reader)
        {
            var result = new List<string[]>();
            
            while (reader.Remaining > 0)
            {
                int valuesCount = (int)reader.ReadLengthEncodedInteger();
                var typeValues = new string[valuesCount];

                for (var i = 0; i < valuesCount; i++)
                {
                    typeValues[i] = reader.ReadLengthEncodedString();
                }

                result.Add(typeValues);
            }

            return result;
        }

        private List<string> ReadStringList(ref SequenceReader<byte> reader)
        {
            var list = new List<string>();

            while (reader.Remaining > 0)
            {
                list.Add(reader.ReadLengthEncodedString());
            }

            return list;
        }

        private List<int> ReadIntegers(ref SequenceReader<byte> reader)
        {
            var charsets = new List<int>();

            while (reader.Remaining > 0)
            {
                charsets.Add((int)reader.ReadLengthEncodedInteger());
            }

            return charsets;
        }

        private Dictionary<int, int> ReadIntegerDictionary(ref SequenceReader<byte> reader)
        {
            var dict = new Dictionary<int, int>();

            while (reader.Remaining > 0)
            {
                dict[(int)reader.ReadLengthEncodedInteger()] = (int)reader.ReadLengthEncodedInteger();
            }

            return dict;
        }

        private DefaultCharset ReadDefaultCharset(ref SequenceReader<byte> reader)
        {
            var charset = new DefaultCharset();
            charset.DefaultCharsetCollation = (int)reader.ReadLengthEncodedInteger();
            
            var dict = ReadIntegerDictionary(ref reader);

            if (dict.Count > 0)
            {
                charset.CharsetCollations = dict;
            }

            return charset;
        }

        private enum MetadataFieldType : byte
        {
            SIGNEDNESS = 1,                   // Signedness of numeric colums
            DEFAULT_CHARSET = 2,                 // Charsets of character columns
            COLUMN_CHARSET = 3,                  // Charsets of character columns
            COLUMN_NAME = 4,                     // Names of columns
            SET_STR_VALUE = 5,                   // The string values of SET columns
            ENUM_STR_VALUE = 6,                  // The string values is ENUM columns
            GEOMETRY_TYPE = 7,                   // The real type of geometry columns
            SIMPLE_PRIMARY_KEY = 8,              // The primary key without any prefix
            PRIMARY_KEY_WITH_PREFIX = 9,         // The primary key with some prefix
            ENUM_AND_SET_DEFAULT_CHARSET = 10,   // Charsets of ENUM and SET columns
            ENUM_AND_SET_COLUMN_CHARSET = 11,    // Charsets of ENUM and SET columns
            COLUMN_VISIBILITY = 12               // Column visibility
        }

    }
}
