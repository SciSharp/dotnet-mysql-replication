using System;
using System.Collections.Generic;

namespace SciSharp.MySQL.Replication
{
    public class DefaultCharset
    {
        public int DefaultCharsetCollation { get; set; }

        public Dictionary<int, int> CharsetCollations { get; set; }
    }
}
