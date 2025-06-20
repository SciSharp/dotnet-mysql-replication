using System;
using System.Security.Cryptography;
using System.Text;

namespace SciSharp.MySQL.Replication.Protocol
{
    /// <summary>
    /// Implements MySQL authentication mechanisms.
    /// </summary>
    internal static class MySQLAuth
    {
        /// <summary>
        /// Implements the mysql_native_password authentication method.
        /// Formula: SHA1(password) XOR SHA1(scramble + SHA1(SHA1(password)))
        /// Reference: https://dev.mysql.com/doc/internals/en/secure-password-authentication.html
        /// </summary>
        public static byte[] MySqlNativePassword(string password, byte[] scramble)
        {
            if (string.IsNullOrEmpty(password))
                return new byte[0];

            if (scramble == null || scramble.Length < 20)
                throw new ArgumentException("Scramble must be at least 20 bytes", nameof(scramble));

            using (var sha1 = SHA1.Create())
            {
                // Step 1: SHA1(password)
                var passwordBytes = Encoding.UTF8.GetBytes(password);
                var sha1Password = sha1.ComputeHash(passwordBytes);

                // Step 2: SHA1(SHA1(password))
                var sha1Sha1Password = sha1.ComputeHash(sha1Password);

                // Step 3: scramble + SHA1(SHA1(password))
                var scrambleAndHash = new byte[20 + sha1Sha1Password.Length];
                Array.Copy(scramble, 0, scrambleAndHash, 0, 20);
                Array.Copy(sha1Sha1Password, 0, scrambleAndHash, 20, sha1Sha1Password.Length);

                // Step 4: SHA1(scramble + SHA1(SHA1(password)))
                var sha1ScrambleAndHash = sha1.ComputeHash(scrambleAndHash);

                // Step 5: SHA1(password) XOR SHA1(scramble + SHA1(SHA1(password)))
                var result = new byte[20];
                for (int i = 0; i < 20; i++)
                {
                    result[i] = (byte)(sha1Password[i] ^ sha1ScrambleAndHash[i]);
                }

                return result;
            }
        }

        /// <summary>
        /// Gets the auth plugin name for mysql_native_password.
        /// </summary>
        public const string MySqlNativePasswordPlugin = "mysql_native_password";
    }
}