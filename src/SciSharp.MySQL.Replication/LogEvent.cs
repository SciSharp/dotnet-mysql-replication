using System;

namespace SciSharp.MySQL.Replication
{
    public abstract class LogEvent
    {
        public DateTime Timestamp { get; set; }
        public LogEventType EventType { get; set; }
        public int ServerID { get; set; }
        public const int MARIA_SLAVE_CAPABILITY_GTID = 4;
        public const int MARIA_SLAVE_CAPABILITY_MINE = MARIA_SLAVE_CAPABILITY_GTID;
    }
}
