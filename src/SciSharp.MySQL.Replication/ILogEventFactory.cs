using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace SciSharp.MySQL.Replication
{
    /// <summary>
    /// Interface for factories that create log events from context objects.
    /// </summary>
    interface ILogEventFactory
    {
        /// <summary>
        /// Creates a log event from the provided context.
        /// </summary>
        /// <param name="context">The context object containing data to create the log event.</param>
        /// <returns>A new log event instance.</returns>
        LogEvent Create(object context);
    }
}
