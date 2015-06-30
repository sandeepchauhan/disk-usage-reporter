using System;
using System.Security.Cryptography;

namespace DiskUsageReporter.Library
{
    public static class StringExtensions
    {
        public static byte[] GetBytes(this string str)
        {
            byte[] bytes = new byte[str.Length * sizeof(char)];
            System.Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }

        public static string SHA1Hash(this string str)
        {
            SHA1 sha1 = SHA1.Create();
            byte[] hashBytes = sha1.ComputeHash(str.GetBytes());
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        }
    }
}
