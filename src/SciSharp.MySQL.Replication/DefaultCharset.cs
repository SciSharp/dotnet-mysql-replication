using System;
using System.Collections.Generic;

namespace SciSharp.MySQL.Replication
{
    /// <summary>
    /// Represents charset information for MySQL replication.
    /// </summary>
    public class DefaultCharset
    {
        /// <summary>
        /// Gets or sets the default collation ID for the charset.
        /// </summary>
        public int DefaultCharsetCollation { get; set; }

        /// <summary>
        /// Gets or sets a dictionary mapping charset IDs to their corresponding collation IDs.
        /// </summary>
        public Dictionary<int, int> CharsetCollations { get; set; }
    }
}
