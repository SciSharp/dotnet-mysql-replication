using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections;
using System.Text;

namespace SciSharp.MySQL.Replication
{
    public class TableMetadata
    {
        public BitArray Signedness { get; set; }
        public DefaultCharset DefaultCharset { get; set; }
        public List<int> ColumnCharsets { get; set; }
        public List<string> ColumnNames { get; set; }
        public List<string[]> SetStrValues { get; set; }
        public List<string[]> EnumStrValues { get; set; }
        public List<int> GeometryTypes { get; set; }
        public List<int> SimplePrimaryKeys { get; set; }
        public Dictionary<int, int> PrimaryKeysWithPrefix { get; set; }
        public DefaultCharset EnumAndSetDefaultCharset { get; set; }
        public List<int> EnumAndSetColumnCharsets { get; set; }
    }
}
