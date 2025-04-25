using System;

namespace SciSharp.MySQL.Replication.Types
{
    /// <summary>
    /// Extension methods for the ColumnType enumeration.
    /// </summary>
    public static class ColumnTypeExtensions
    {
        /// <summary>
        /// Determines if the specified column type is a numeric type.
        /// </summary>
        /// <param name="columnType">The column type.</param>
        internal static bool IsNumberColumn(this ColumnType columnType)
        {
            switch (columnType)
            {
                case ColumnType.TINY:
                case ColumnType.SHORT:
                case ColumnType.INT24:
                case ColumnType.LONG:
                case ColumnType.LONGLONG:
                case ColumnType.NEWDECIMAL:
                case ColumnType.FLOAT:
                case ColumnType.DOUBLE:
                    return true;
                default:
                    return false;
            }
        }
    }
}