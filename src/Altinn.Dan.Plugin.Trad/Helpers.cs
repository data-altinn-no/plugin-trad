using System;
using System.Security.Cryptography;
using System.Text;

namespace Altinn.Dan.Plugin.Trad
{
    public static class Helpers
    {
        private static string KEY_PREFIX = "trad-entry-";

        public static string GetCacheKeyForSsn(string ssn)
        {
            using var sha256 = SHA256.Create();
            return KEY_PREFIX + Convert.ToBase64String(sha256.ComputeHash(Encoding.UTF8.GetBytes(ssn)));
        }
    }
}
