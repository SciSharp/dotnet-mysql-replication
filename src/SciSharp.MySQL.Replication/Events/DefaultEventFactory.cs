using System;
using System.Buffers;

namespace SciSharp.MySQL.Replication
{
    /// <summary>
    /// Default implementation of the log event factory that creates events of a specified type.
    /// </summary>
    /// <typeparam name="TEventType">The type of log event to create.</typeparam>
    class DefaultEventFactory<TEventType> : ILogEventFactory
        where TEventType : LogEvent, new()
    {
        /// <summary>
        /// Creates a new instance of a log event.
        /// </summary>
        /// <param name="context">The context information for creating the event.</param>
        /// <returns>A new instance of the specified log event type.</returns>
        public LogEvent Create(object context)
        {
            return new TEventType();
        }
    }
}
