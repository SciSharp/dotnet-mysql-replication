using System;
using System.Buffers;
using System.Collections.Generic;
using SciSharp.MySQL.Replication.Events;
using SuperSocket.ProtoBase;

namespace SciSharp.MySQL.Replication
{
    /// <summary>
    /// Decoder for MySQL binary log events.
    /// See: https://dev.mysql.com/doc/internals/en/binlog-event.html
    /// and: https://dev.mysql.com/doc/dev/mysql-server/latest/page_protocol_replication_binlog_event.html
    /// </summary>
    class LogEventPackageDecoder : IPackageDecoder<LogEvent>
    {
        /// <summary>
        /// Dictionary of log event factories indexed by event type.
        /// </summary>
        private static Dictionary<LogEventType, ILogEventFactory> _logEventFactories = new Dictionary<LogEventType, ILogEventFactory>();
        
        /// <summary>
        /// Default factory for events that are not implemented.
        /// </summary>
        private static ILogEventFactory _notImplementedEventFactory = new DefaultEventFactory<NotImplementedEvent>();
        
        /// <summary>
        /// Registers a log event type with a default factory.
        /// </summary>
        /// <typeparam name="TLogEvent">Type of log event to register.</typeparam>
        /// <param name="eventType">The event type to register.</param>
        internal static void RegisterLogEventType<TLogEvent>(LogEventType eventType)
            where TLogEvent : LogEvent, new()
        {
            RegisterLogEventType(eventType, new DefaultEventFactory<TLogEvent>());
        }

        /// <summary>
        /// Registers a log event type with a custom factory.
        /// </summary>
        /// <param name="eventType">The event type to register.</param>
        /// <param name="factory">The factory to use for creating events of this type.</param>
        internal static void RegisterLogEventType(LogEventType eventType, ILogEventFactory factory)
        {
            _logEventFactories.Add(eventType, factory);
        }

        /// <summary>
        /// Registers multiple event types that have empty payloads.
        /// </summary>
        /// <param name="eventTypes">The event types to register as empty payload events.</param>
        internal static void RegisterEmptyPayloadEventTypes(params LogEventType[] eventTypes)
        {
            foreach (var eventType in eventTypes)
            {
                _logEventFactories.Add(eventType, new DefaultEventFactory<EmptyPayloadEvent>());
            }
        }

        /// <summary>
        /// Creates a log event instance of the specified type.
        /// </summary>
        /// <param name="eventType">The type of log event to create.</param>
        /// <param name="context">The context object for the event.</param>
        /// <returns>A new log event instance.</returns>
        protected virtual LogEvent CreateLogEvent(LogEventType eventType, object context)
        {
            if (!_logEventFactories.TryGetValue(eventType, out var factory))
                factory = _notImplementedEventFactory;

            var log = factory.Create(context);
            log.EventType = eventType;
            return log;
        }

        /// <summary>
        /// Decodes a binary buffer into a LogEvent object.
        /// </summary>
        /// <param name="buffer">The buffer containing binary data.</param>
        /// <param name="context">The context object for the decoding.</param>
        /// <returns>A decoded LogEvent object.</returns>
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
