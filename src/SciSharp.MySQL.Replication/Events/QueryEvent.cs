using System;
using System.Buffers;
using System.Text;
using SuperSocket.ProtoBase;

namespace SciSharp.MySQL.Replication
{
    /// <summary>
    /// Represents a MySQL QUERY_EVENT that contains SQL statements executed on the server.
    /// This event is generated for statements like CREATE, ALTER, INSERT, UPDATE, DELETE, etc.
    /// that are not using row-based replication format.
    /// </summary>
    public sealed class QueryEvent : LogEvent
    {
        /// <summary>
        /// Gets or sets the ID of the thread that executed the query.
        /// </summary>
        public int SlaveProxyID { get; private set; }

        /// <summary>
        /// Gets or sets the time in seconds the query took to execute.
        /// </summary>
        public DateTime ExecutionTime { get; private set; }

        /// <summary>
        /// Gets or sets the error code returned by the query execution.
        /// A value of 0 indicates successful execution.
        /// </summary>
        public short ErrorCode { get; private set; }

        /// <summary>
        /// Gets or sets the status variables block.
        /// </summary>
        public string StatusVars { get; private set; }

        /// <summary>
        /// Gets or sets the database name that was active when the query was executed.
        /// </summary>
        public string Schema { get; private set; }

        /// <summary>
        /// Gets or sets the SQL query text that was executed.
        /// </summary>
        public String Query { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryEvent"/> class.
        /// </summary>
        public QueryEvent()
        {
            this.HasCRC = true;
        }

        /// <summary>
        /// Decodes the body of the event from the binary representation.
        /// </summary>
        /// <param name="reader">The sequence reader containing the binary data.</param>
        /// <param name="context">The context for decoding.</param>
        protected internal override void DecodeBody(ref SequenceReader<byte> reader, object context)
        {
            reader.TryReadLittleEndian(out int slaveProxyID);
            SlaveProxyID = slaveProxyID;

            reader.TryReadLittleEndian(out int seconds);
            ExecutionTime = LogEvent.GetTimestampFromUnixEpoch(seconds);

            reader.TryRead(out byte schemaLen);

            reader.TryReadLittleEndian(out short errorCode);
            ErrorCode = errorCode;

            reader.TryReadLittleEndian(out short statusVarsLen);

            StatusVars = reader.ReadString(Encoding.UTF8, statusVarsLen);

            Schema = reader.ReadString(Encoding.UTF8, schemaLen);

            reader.Advance(1); //0x00

            this.RebuildReaderAsCRC(ref reader);

            Query = reader.ReadString();
        }

        /// <summary>
        /// Returns a string representation of the QueryEvent.
        /// </summary>
        /// <returns>A string containing the event type, database name, and SQL query.</returns>
        public override string ToString()
        {
            return $"{EventType.ToString()}\r\nSlaveProxyID: {SlaveProxyID}\r\nExecutionTime: {ExecutionTime}\r\nErrorCode: {ErrorCode}\r\nStatusVars: {StatusVars}\r\nSchema: {Schema}\r\nQuery: {Query}";
        }
    }
}
