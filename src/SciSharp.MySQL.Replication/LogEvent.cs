using System;

namespace SciSharp.MySQL.Replication
{
    public abstract class LogEvent
    {
        public DateTime Timestamp { get; set; }
        public LogEventType EventType { get; set; }
        public int ServerID { get; set; }
    }
}
