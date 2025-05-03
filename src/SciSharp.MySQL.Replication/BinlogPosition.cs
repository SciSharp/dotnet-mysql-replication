using System;

namespace SciSharp.MySQL.Replication
{
    /// <summary>
    /// Represents a position in a MySQL binary log file.
    /// </summary>
    public class BinlogPosition
    {
        /// <summary>
        /// Gets or sets the filename of the binary log.
        /// </summary>
        public string Filename { get; set; }

        /// <summary>
        /// Gets or sets the position within the binary log file.
        /// </summary>
        public int Position { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BinlogPosition"/> class.
        /// </summary>
        public BinlogPosition()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BinlogPosition"/> class by copying another instance.
        /// </summary>
        /// <param name="binlogPosition">The other binlogPosition.</param>
        public BinlogPosition(BinlogPosition binlogPosition)
        {
            Filename = binlogPosition.Filename;
            Position = binlogPosition.Position;
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="BinlogPosition"/> class.
        /// </summary>
        /// <param name="filename">The binary log filename.</param>
        /// <param name="position">The position within the binary log file.</param>
        public BinlogPosition(string filename, int position)
        {
            Filename = filename ?? throw new ArgumentNullException(nameof(filename));
            Position = position;
        }

        /// <summary>
        /// Returns a string representation of the binlog position.
        /// </summary>
        /// <returns>A string containing the filename and position.</returns>
        public override string ToString()
        {
            return $"{Filename}:{Position}";
        }
    }
}