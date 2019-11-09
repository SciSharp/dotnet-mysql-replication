using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace SciSharp.MySQL.Replication
{
    public enum ColumnType : byte
    {
        DECIMAL = 0,
        TINY = 1,
        SHORT = 2,
        LONG = 3,
        FLOAT = 4,
        DOUBLE = 5,
        NULL = 6,
        TIMESTAMP = 7,
        LONGLONG = 8,
        INT24 = 9,
        DATE = 10,
        TIME = 11,
        DATETIME = 12,
        YEAR = 13,
        NEWDATE = 14,
        VARCHAR = 15,
        BIT = 16,
        TIMESTAMP_V2 = 17,
        DATETIME_V2 = 18,
        TIME_V2 = 19,
        JSON = 245,
        NEWDECIMAL = 246,
        ENUM = 247,
        SET = 248,
        TINY_BLOB = 249,
        MEDIUM_BLOB = 250,
        LONG_BLOB = 251,
        BLOB = 252,
        VAR_STRING = 253,
        STRING = 254,
        GEOMETRY = 255
    }
}
