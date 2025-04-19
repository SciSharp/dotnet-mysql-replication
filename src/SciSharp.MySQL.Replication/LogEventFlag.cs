using System;

namespace SciSharp.MySQL.Replication
{
    /// <summary>
    /// Defines the flags used in binary log events.
    /// See: https://dev.mysql.com/doc/internals/en/binlog-event-flag.html
    /// </summary>
    [Flags]
    public enum LogEventFlag : short
    {
        /// <summary>
        /// Indicates a binlog is in use.
        /// </summary>
        LOG_EVENT_BINLOG_IN_USE_F = 0x001,

        /// <summary>
        /// Indicates a forced rotation of the binlog.
        /// </summary>
        LOG_EVENT_FORCED_ROTATE_F = 0x0002,

        /// <summary>
        /// Indicates the event is thread specific.
        /// </summary>
        LOG_EVENT_THREAD_SPECIFIC_F = 0x0004,

        /// <summary>
        /// Indicates to suppress the USE command.
        /// </summary>
        LOG_EVENT_SUPPRESS_USE_F = 0x0008,

        /// <summary>
        /// Indicates that table map version is updated.
        /// </summary>
        LOG_EVENT_UPDATE_TABLE_MAP_VERSION_F = 0x0010,

        /// <summary>
        /// Indicates the event was artificially generated.
        /// </summary>
        LOG_EVENT_ARTIFICIAL_F = 0x0020,

        /// <summary>
        /// Indicates the event is a relay log event.
        /// </summary>
        LOG_EVENT_RELAY_LOG_F = 0x0040,

        /// <summary>
        /// Indicates the event can be safely ignored.
        /// </summary>
        LOG_EVENT_IGNORABLE_F = 0x0080,

        /// <summary>
        /// Indicates no filtering should be applied to the event.
        /// </summary>
        LOG_EVENT_NO_FILTER_F = 0x0100,

        /// <summary>
        /// Indicates the event should be isolated in MTS operation.
        /// </summary>
        LOG_EVENT_MTS_ISOLATE_F = 0x0200
    }
}
