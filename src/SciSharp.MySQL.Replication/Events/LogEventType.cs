using System;

namespace SciSharp.MySQL.Replication.Events
{
    /// <summary>
    /// Represents the different types of events in a MySQL binary log.
    /// See: https://dev.mysql.com/doc/internals/en/binlog-event-type.html
    /// </summary>
    public enum LogEventType : byte
    {
        /// <summary>Unknown event type</summary>
        UNKNOWN_EVENT= 0, 
        /// <summary>Start event version 3</summary>
        START_EVENT_V3= 1, 
        /// <summary>Query event containing SQL statement</summary>
        QUERY_EVENT= 2, 
        /// <summary>Stop event</summary>
        STOP_EVENT= 3, 
        /// <summary>Rotate event indicating a new binary log</summary>
        ROTATE_EVENT= 4, 
        /// <summary>Internal variable event</summary>
        INTVAR_EVENT= 5, 
        /// <summary>Load data event</summary>
        LOAD_EVENT= 6, 
        /// <summary>Slave event</summary>
        SLAVE_EVENT= 7, 
        /// <summary>Create file event</summary>
        CREATE_FILE_EVENT= 8, 
        /// <summary>Append block event</summary>
        APPEND_BLOCK_EVENT= 9, 
        /// <summary>Execute load event</summary>
        EXEC_LOAD_EVENT= 10, 
        /// <summary>Delete file event</summary>
        DELETE_FILE_EVENT= 11, 
        /// <summary>New load event</summary>
        NEW_LOAD_EVENT= 12, 
        /// <summary>Random value event</summary>
        RAND_EVENT= 13, 
        /// <summary>User variable event</summary>
        USER_VAR_EVENT= 14, 
        /// <summary>Format description event containing binary log format information</summary>
        FORMAT_DESCRIPTION_EVENT= 15, 
        /// <summary>XID transaction identifier event</summary>
        XID_EVENT= 16, 
        /// <summary>Begin load query event</summary>
        BEGIN_LOAD_QUERY_EVENT= 17, 
        /// <summary>Execute load query event</summary>
        EXECUTE_LOAD_QUERY_EVENT= 18, 
        /// <summary>Table map event describing a table structure</summary>
        TABLE_MAP_EVENT = 19, 
        /// <summary>Write rows event version 0</summary>
        WRITE_ROWS_EVENT_V0 = 20, 
        /// <summary>Update rows event version 0</summary>
        UPDATE_ROWS_EVENT_V0 = 21, 
        /// <summary>Delete rows event version 0</summary>
        DELETE_ROWS_EVENT_V0 = 22, 
        /// <summary>Write rows event version 1</summary>
        WRITE_ROWS_EVENT_V1 = 23, 
        /// <summary>Update rows event version 1</summary>
        UPDATE_ROWS_EVENT_V1 = 24, 
        /// <summary>Delete rows event version 1</summary>
        DELETE_ROWS_EVENT_V1 = 25, 
        /// <summary>Incident event indicating a server issue</summary>
        INCIDENT_EVENT= 26, 
        /// <summary>Heartbeat log event</summary>
        HEARTBEAT_LOG_EVENT= 27, 
        /// <summary>Ignorable log event</summary>
        IGNORABLE_LOG_EVENT= 28,
        /// <summary>Rows query log event containing the original SQL statement</summary>
        ROWS_QUERY_LOG_EVENT= 29,
        /// <summary>Write rows event (current version)</summary>
        WRITE_ROWS_EVENT = 30,
        /// <summary>Update rows event (current version)</summary>
        UPDATE_ROWS_EVENT = 31,
        /// <summary>Delete rows event (current version)</summary>
        DELETE_ROWS_EVENT = 32,
        /// <summary>GTID (Global Transaction ID) log event</summary>
        GTID_LOG_EVENT= 33,
        /// <summary>Anonymous GTID log event</summary>
        ANONYMOUS_GTID_LOG_EVENT= 34,
        /// <summary>Previous GTIDs log event</summary>
        PREVIOUS_GTIDS_LOG_EVENT= 35
    }
}
