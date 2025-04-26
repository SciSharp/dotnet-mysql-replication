using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SciSharp.MySQL.Replication.Types;
using SuperSocket.ProtoBase;

namespace SciSharp.MySQL.Replication.Events
{
    /// <summary>
    /// Represents a MySQL TABLE_MAP_EVENT that contains information about a table's structure.
    /// This event precedes row events and provides metadata needed to interpret the row data.
    /// https://dev.mysql.com/doc/internals/en/table-map-event.html
    /// </summary>
    public sealed class TableMapEvent : LogEvent
    {
        /// <summary>
        /// Gets or sets the table ID.
        /// </summary>
        public long TableID { get; set; }

        /// <summary>
        /// Gets or sets the database name.
        /// </summary>
        public string SchemaName { get; set; }

        /// <summary>
        /// Gets or sets the table name.
        /// </summary>
        public string TableName { get; set; }

        /// <summary>
        /// Gets or sets the number of columns in the table.
        /// </summary>
        public int ColumnCount { get; set; }

        /// <summary>
        /// Gets or sets the array of column types.
        /// </summary>
        public byte[] ColumnTypes { get; set; }

        /// <summary>
        /// Gets or sets the array of metadata for each column.
        /// </summary>
        public int[] ColumnMetadata { get; set; }        
        
        /// <summary>
        /// Gets or sets the bitmap of columns that can be null.
        /// </summary>
        public BitArray NullBitmap { get; set; }

        /// <summary>
        /// Gets or sets the metadata of the table.
        /// </summary>
        public TableMetadata Metadata { get; set; }

        private readonly IReadOnlyList<ITableMetadataInitializer> _tableMetadataInitializers = new List<ITableMetadataInitializer>
        {
            new NumericTypeTableMetadataInitializer(),
            new EnumTypeTableMetadataInitializer(),
            new SetTypeTableMetadataInitializer()
        };
        
        /// <summary>
        /// Initializes a new instance of the <see cref="TableMapEvent"/> class.
        /// </summary>
        public TableMapEvent()
        {
            HasCRC = true;
        }

        /// <summary>
        /// Decodes the body of the event from the binary representation.
        /// </summary>
        /// <param name="reader">The sequence reader containing the binary data.</param>
        /// <param name="context">The context for decoding.</param>
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

            foreach (var tableMetadataInitializer in _tableMetadataInitializers)
            {
                tableMetadataInitializer.InitializeMetadata(Metadata);
            }

            foreach (var columnMetadata in Metadata.Columns)
            {
                var valueTypeIndex = (int)columnMetadata.Type;

                if (valueTypeIndex < DataTypes.Length && DataTypes[valueTypeIndex] is IColumnMetadataLoader columnMetadataLoader)
                {
                    columnMetadataLoader.LoadMetadataValue(columnMetadata);
                }
            }

            if (context is ReplicationState repState)
            {
                repState.TableMap[TableID] = this;
            }
        }

        /// <summary>
        /// Returns a string representation of the TableMapEvent.
        /// </summary>
        /// <returns>A string containing the event type, table ID, schema name, table name, and column count.</returns>
        public override string ToString()
        {
            return $"{EventType.ToString()}\r\nTableID: {TableID}\r\nSchemaName: {SchemaName}\r\nTableName: {TableName}\r\nColumnCount: {ColumnCount}";
        }

        /// <summary>
        /// Reads the metadata for each column from the binary representation.
        /// </summary>
        /// <param name="reader">The sequence reader containing the binary data.</param>
        /// <param name="columnTypes">The array of column types.</param>
        /// <returns>An array of metadata for each column.</returns>
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

        /// <summary>
        /// Reads the table metadata from the binary representation.
        /// </summary>
        /// <param name="reader">The sequence reader containing the binary data.</param>
        /// <returns>The table metadata.</returns>
        private TableMetadata ReadTableMetadata(ref SequenceReader<byte> reader)
        {
            var numericColumnCount = ColumnTypes.Count(c => ((ColumnType)c).IsNumberColumn());

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

            metadata.BuildColumnMetadataList(ColumnTypes.Select(c => (ColumnType)c).ToArray(), ColumnMetadata);

            return metadata;
        }

        /// <summary>
        /// Reads a metadata field from the binary representation.
        /// </summary>
        /// <param name="subReader">The sequence reader containing the binary data.</param>
        /// <param name="fieldType">The type of the metadata field.</param>
        /// <param name="metadata">The table metadata to update.</param>
        /// <param name="numericColumnCount">The number of numeric columns.</param>
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

        /// <summary>
        /// Reads the type values for ENUM and SET columns from the binary representation.
        /// </summary>
        /// <param name="reader">The sequence reader containing the binary data.</param>
        /// <returns>A list of string arrays representing the type values.</returns>
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

        /// <summary>
        /// Reads a list of strings from the binary representation.
        /// </summary>
        /// <param name="reader">The sequence reader containing the binary data.</param>
        /// <returns>A list of strings.</returns>
        private List<string> ReadStringList(ref SequenceReader<byte> reader)
        {
            var list = new List<string>();

            while (reader.Remaining > 0)
            {
                list.Add(reader.ReadLengthEncodedString());
            }

            return list;
        }

        /// <summary>
        /// Reads a list of integers from the binary representation.
        /// </summary>
        /// <param name="reader">The sequence reader containing the binary data.</param>
        /// <returns>A list of integers.</returns>
        private List<int> ReadIntegers(ref SequenceReader<byte> reader)
        {
            var charsets = new List<int>();

            while (reader.Remaining > 0)
            {
                charsets.Add((int)reader.ReadLengthEncodedInteger());
            }

            return charsets;
        }

        /// <summary>
        /// Reads a dictionary of integers from the binary representation.
        /// </summary>
        /// <param name="reader">The sequence reader containing the binary data.</param>
        /// <returns>A dictionary of integers.</returns>
        private Dictionary<int, int> ReadIntegerDictionary(ref SequenceReader<byte> reader)
        {
            var dict = new Dictionary<int, int>();

            while (reader.Remaining > 0)
            {
                dict[(int)reader.ReadLengthEncodedInteger()] = (int)reader.ReadLengthEncodedInteger();
            }

            return dict;
        }

        /// <summary>
        /// Reads the default charset from the binary representation.
        /// </summary>
        /// <param name="reader">The sequence reader containing the binary data.</param>
        /// <returns>The default charset.</returns>
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

        /// <summary>
        /// Represents the types of metadata fields in a table map event.
        /// </summary>
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
