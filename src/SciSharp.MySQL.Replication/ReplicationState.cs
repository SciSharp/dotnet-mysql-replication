using System;
using System.Collections.Generic;

namespace SciSharp.MySQL.Replication
{
    class ReplicationState
    {

        public Dictionary<long, TableMapEvent> TableMap { get; set; } = new Dictionary<long, TableMapEvent>();
    }
}
