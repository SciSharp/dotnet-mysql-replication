using System;
using System.Collections.Generic;
using SciSharp.MySQL.Replication.Events;

namespace SciSharp.MySQL.Replication
{
    /// <summary>
    /// Represents the state information maintained during a MySQL replication session.
    /// </summary>
    class ReplicationState
    {
        /// <summary>
        /// Gets or sets the dictionary mapping table IDs to their corresponding TableMapEvent objects.
        /// </summary>
        public Dictionary<long, TableMapEvent> TableMap { get; set; } = new Dictionary<long, TableMapEvent>();
    }
}
