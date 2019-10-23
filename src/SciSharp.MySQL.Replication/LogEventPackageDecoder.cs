using System;
using System.Buffers;
using System.Collections.Generic;
using SuperSocket.ProtoBase;

namespace SciSharp.MySQL.Replication
{
    /*
    //https://dev.mysql.com/doc/internals/en/binlog-event.html
    //https://dev.mysql.com/doc/dev/mysql-server/latest/page_protocol_replication_binlog_event.html
    */
    class LogEventPackageDecoder : IPackageDecoder<LogEvent>
    {
        class DefaultLogEventFactory<TLogEvent> : ILogEventFactory
            where TLogEvent : LogEvent, new()
        {
            public LogEvent Create()
            {
                return new TLogEvent();
            }
        }

        private static readonly DateTime _unixEpoch = new DateTime(1970, 1, 1);

        private static Dictionary<LogEventType, ILogEventFactory> _logEventFactories = new Dictionary<LogEventType, ILogEventFactory>();

        internal static void RegisterLogEventType<TLogEvent>(LogEventType eventType)
            where TLogEvent : LogEvent, new()
        {
            RegisterLogEventType(eventType, new DefaultLogEventFactory<TLogEvent>());
        }

        internal static void RegisterLogEventType(LogEventType eventType, ILogEventFactory factory)
        {
            _logEventFactories.Add(eventType, factory);
        }

        internal static void RegisterEmptyPayloadEventTypes(params LogEventType[] eventTypes)
        {
            foreach (var eventType in eventTypes)
            {
                _logEventFactories.Add(eventType, new EmptyPayloadEventFactory(eventType));
            }
        }

        protected virtual LogEvent CreateLogEvent(LogEventType eventType)
        {
            if (!_logEventFactories.TryGetValue(eventType, out var factory))
                throw new Exception("Unexpected eventType: " + eventType);

            return factory.Create();
        }

        public LogEvent Decode(ReadOnlySequence<byte> buffer)
        {
            var reader = new SequenceReader<byte>(buffer);

            reader.TryReadBigEndian(out int seconds);
            var timestamp = _unixEpoch.AddSeconds(seconds);

            reader.TryRead(out byte eventTypeValue);
            var eventType = (LogEventType)eventTypeValue;

            var log = CreateLogEvent(eventType);

            log.Timestamp = timestamp;
            log.EventType = eventType;

            reader.TryReadBigEndian(out int serverID);
            log.ServerID = serverID;

            reader.TryReadBigEndian(out int position);
            log.Position = position;
            
            reader.TryReadBigEndian(out short flags);
            log.Flags = (LogEventFlag)flags;

            log.DecodeBody(ref reader);

            return log;
        }
    }
}
