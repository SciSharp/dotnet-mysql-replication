using System;

namespace SciSharp.MySQL.Replication
{
    /// <summary>
    /// Represents the result of a login attempt to a MySQL server.
    /// </summary>
    public class LoginResult
    {
        /// <summary>
        /// Gets or sets a value indicating whether the login was successful.
        /// </summary>
        public bool Result { get; set; }
        
        /// <summary>
        /// Gets or sets a message describing the login result.
        /// </summary>
        public string Message { get; set; }
    }
}
