using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace SciSharp.MySQL.Replication
{
    /// <summary>
    /// Represents the MySQL column data types as they appear in the binary log format.
    /// These values correspond to the type codes used in TABLE_MAP_EVENT to describe table columns.
    /// </summary>
    /// <remarks>
    /// The values match those defined in the MySQL source code in the enum_field_types enumeration,
    /// which can be found in mysql-server/include/mysql_com.h.
    /// </remarks>
    public enum ColumnType : byte
    {
        /// <summary>
        /// MySQL DECIMAL type (old precision-based format, rarely used).
        /// </summary>
        DECIMAL = 0,
        
        /// <summary>
        /// MySQL TINY (TINYINT) type, 1-byte integer.
        /// </summary>
        TINY = 1,
        
        /// <summary>
        /// MySQL SHORT (SMALLINT) type, 2-byte integer.
        /// </summary>
        SHORT = 2,
        
        /// <summary>
        /// MySQL LONG (INT) type, 4-byte integer.
        /// </summary>
        LONG = 3,
        
        /// <summary>
        /// MySQL FLOAT type, 4-byte single-precision floating point.
        /// </summary>
        FLOAT = 4,
        
        /// <summary>
        /// MySQL DOUBLE type, 8-byte double-precision floating point.
        /// </summary>
        DOUBLE = 5,
        
        /// <summary>
        /// MySQL NULL type, represents NULL values.
        /// </summary>
        NULL = 6,
        
        /// <summary>
        /// MySQL TIMESTAMP type, represents a point in time.
        /// </summary>
        TIMESTAMP = 7,
        
        /// <summary>
        /// MySQL LONGLONG (BIGINT) type, 8-byte integer.
        /// </summary>
        LONGLONG = 8,
        
        /// <summary>
        /// MySQL INT24 (MEDIUMINT) type, 3-byte integer.
        /// </summary>
        INT24 = 9,
        
        /// <summary>
        /// MySQL DATE type, represents a calendar date.
        /// </summary>
        DATE = 10,
        
        /// <summary>
        /// MySQL TIME type, represents a time of day.
        /// </summary>
        TIME = 11,
        
        /// <summary>
        /// MySQL DATETIME type, represents a combined date and time.
        /// </summary>
        DATETIME = 12,
        
        /// <summary>
        /// MySQL YEAR type, 1-byte representation of a year.
        /// </summary>
        YEAR = 13,
        
        /// <summary>
        /// MySQL NEWDATE type, new internal representation of DATE (rarely used externally).
        /// </summary>
        NEWDATE = 14,
        
        /// <summary>
        /// MySQL VARCHAR type, variable-length character string.
        /// </summary>
        VARCHAR = 15,
        
        /// <summary>
        /// MySQL BIT type, for storing bit values.
        /// </summary>
        BIT = 16,
        
        /// <summary>
        /// MySQL TIMESTAMP2 type, timestamp with fractional seconds, introduced in MySQL 5.6.4.
        /// </summary>
        TIMESTAMP_V2 = 17,
        
        /// <summary>
        /// MySQL DATETIME2 type, datetime with fractional seconds, introduced in MySQL 5.6.4.
        /// </summary>
        DATETIME_V2 = 18,
        
        /// <summary>
        /// MySQL TIME2 type, time with fractional seconds, introduced in MySQL 5.6.4.
        /// </summary>
        TIME_V2 = 19,
        
        /// <summary>
        /// MySQL JSON type for storing JSON documents, introduced in MySQL 5.7.
        /// </summary>
        JSON = 245,
        
        /// <summary>
        /// MySQL NEWDECIMAL type, new precision-based decimal implementation.
        /// </summary>
        NEWDECIMAL = 246,
        
        /// <summary>
        /// MySQL ENUM type, enumeration of string values.
        /// </summary>
        ENUM = 247,
        
        /// <summary>
        /// MySQL SET type, string object that can have zero or more values.
        /// </summary>
        SET = 248,
        
        /// <summary>
        /// MySQL TINY_BLOB type, small binary object.
        /// </summary>
        TINY_BLOB = 249,
        
        /// <summary>
        /// MySQL MEDIUM_BLOB type, medium-sized binary object.
        /// </summary>
        MEDIUM_BLOB = 250,
        
        /// <summary>
        /// MySQL LONG_BLOB type, large binary object.
        /// </summary>
        LONG_BLOB = 251,
        
        /// <summary>
        /// MySQL BLOB type, binary large object.
        /// </summary>
        BLOB = 252,
        
        /// <summary>
        /// MySQL VAR_STRING type, variable-length string (deprecated, VARCHAR is used instead).
        /// </summary>
        VAR_STRING = 253,
        
        /// <summary>
        /// MySQL STRING type, fixed-length string.
        /// </summary>
        STRING = 254,
        
        /// <summary>
        /// MySQL GEOMETRY type, for storing geometric data.
        /// </summary>
        GEOMETRY = 255
    }
}
