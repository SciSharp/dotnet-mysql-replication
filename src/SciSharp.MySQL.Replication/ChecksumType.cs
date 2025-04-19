using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace SciSharp.MySQL.Replication
{
    /// <summary>
    /// Defines the types of checksums used in MySQL replication events.
    /// </summary>
    public enum ChecksumType : int
    {
        /// <summary>
        /// No checksum is used.
        /// </summary>
        NONE = 0,
        
        /// <summary>
        /// CRC32 checksum algorithm is used.
        /// </summary>
        CRC32 = 4
    }
}
