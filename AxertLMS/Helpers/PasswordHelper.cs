using System;
using System.Security.Cryptography;
using System.Text;
using BCrypt.Net;
using Microsoft.Extensions.Logging;

namespace AxertLMS.Helpers
{
    public static class PasswordHelper
    {
        private const int BCryptWorkFactor = 12;

        public static string Hash(string rawPassword)
        {
            if (string.IsNullOrEmpty(rawPassword))
            {
                throw new ArgumentException(
                    "Password cannot be empty.",
                    nameof(rawPassword));
            }

            return BCrypt.Net.BCrypt.HashPassword(
                rawPassword,
                BCryptWorkFactor);
        }

        public static bool VerifyBCrypt(
            string rawPassword,
            string databaseHash,
            ILogger logger)
        {
            if (string.IsNullOrEmpty(rawPassword) ||
                string.IsNullOrEmpty(databaseHash))
            {
                return false;
            }

            try
            {
                return BCrypt.Net.BCrypt.Verify(
                    rawPassword,
                    databaseHash);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "CRYPTOGRAPHY_ERROR: BCrypt verification failed.");

                return false;
            }
        }

        public static bool IsLegacySha256Hash(
            string? hash)
        {
            if (string.IsNullOrWhiteSpace(hash))
            {
                return false;
            }

            if (hash.Length != 64)
            {
                return false;
            }

            foreach (char c in hash)
            {
                bool validHex =
                    (c >= '0' && c <= '9') ||
                    (c >= 'a' && c <= 'f') ||
                    (c >= 'A' && c <= 'F');

                if (!validHex)
                {
                    return false;
                }
            }

            return true;
        }

        public static string HashSHA256(
            string rawPassword)
        {
            using SHA256 sha256 =
                SHA256.Create();

            byte[] bytes =
                sha256.ComputeHash(
                    Encoding.UTF8.GetBytes(
                        rawPassword));

            StringBuilder builder =
                new StringBuilder(
                    bytes.Length * 2);

            foreach (byte b in bytes)
            {
                builder.Append(
                    b.ToString("x2"));
            }

            return builder.ToString();
        }
    }
}