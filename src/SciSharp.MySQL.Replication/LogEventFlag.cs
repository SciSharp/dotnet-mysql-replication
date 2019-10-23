using System;

namespace SciSharp.MySQL.Replication
{
    /*
    /* https://dev.mysql.com/doc/internals/en/binlog-event-flag.html
    */
    [Flags]
    public enum LogEventFlag : short
    {
        LOG_EVENT_BINLOG_IN_USE_F = 0x001,

        LOG_EVENT_FORCED_ROTATE_F = 0x0002,

        LOG_EVENT_THREAD_SPECIFIC_F = 0x0004,

        LOG_EVENT_SUPPRESS_USE_F = 0x0008,

        LOG_EVENT_UPDATE_TABLE_MAP_VERSION_F = 0x0010,

        LOG_EVENT_ARTIFICIAL_F = 0x0020,

        LOG_EVENT_RELAY_LOG_F = 0x0040,

        LOG_EVENT_IGNORABLE_F = 0x0080,

        LOG_EVENT_NO_FILTER_F = 0x0100,

        LOG_EVENT_MTS_ISOLATE_F = 0x0200
    }
}
