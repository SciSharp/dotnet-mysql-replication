using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace SciSharp.MySQL.Replication
{
    interface ILogEventFactory
    {
        LogEvent Create();
    }
}
