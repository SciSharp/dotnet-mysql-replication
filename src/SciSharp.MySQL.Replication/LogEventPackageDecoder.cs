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

        private static Dictionary<LogEventType, ILogEventFactory> _logEventFactories = new Dictionary<LogEventType, ILogEventFactory>();
        private static ILogEventFactory _notImplementedEventFactory = new DefaultEventFactory<NotImplementedEvent>();
        internal static void RegisterLogEventType<TLogEvent>(LogEventType eventType)
            where TLogEvent : LogEvent, new()
        {
            RegisterLogEventType(eventType, new DefaultEventFactory<TLogEvent>());
        }

        internal static void RegisterLogEventType(LogEventType eventType, ILogEventFactory factory)
        {
            _logEventFactories.Add(eventType, factory);
        }

        internal static void RegisterEmptyPayloadEventTypes(params LogEventType[] eventTypes)
        {
            foreach (var eventType in eventTypes)
            {
                _logEventFactories.Add(eventType, new DefaultEventFactory<EmptyPayloadEvent>());
            }
        }

        protected virtual LogEvent CreateLogEvent(LogEventType eventType, object context)
        {
            if (!_logEventFactories.TryGetValue(eventType, out var factory))
                factory = _notImplementedEventFactory;

            var log = factory.Create(context);
            log.EventType = eventType;
            return log;
        }

        public LogEvent Decode(ref ReadOnlySequence<byte> buffer, object context)
        {
            var reader = new SequenceReader<byte>(buffer);

            reader.Advance(4); // 3 + 1

            // ok byte
            reader.TryRead(out byte ok);

            if (ok == 0xFF)
            {
                var errorLogEvent = new ErrorEvent();
                errorLogEvent.DecodeBody(ref reader, context);
                return errorLogEvent;
            }

            reader.TryReadLittleEndian(out int seconds);
            var timestamp = LogEvent.GetTimestampFromUnixEpoch(seconds);

            reader.TryRead(out byte eventTypeValue);
            var eventType = (LogEventType)eventTypeValue;

            var log = CreateLogEvent(eventType, context);

            log.Timestamp = timestamp;
            log.EventType = eventType;

            reader.TryReadLittleEndian(out int serverID);
            log.ServerID = serverID;

            reader.TryReadLittleEndian(out int eventSize);
            log.EventSize = eventSize;

            reader.TryReadLittleEndian(out int position);
            log.Position = position;
            
            reader.TryReadLittleEndian(out short flags);
            log.Flags = (LogEventFlag)flags;

            log.DecodeBody(ref reader, context);

            return log;
        }
    }
}
