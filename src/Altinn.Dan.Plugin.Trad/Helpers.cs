using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Grpc.Core;

namespace Altinn.Dan.Plugin.Trad
{
    public static class Helpers
    {
        public static string GetCacheKeyForSsn(string ssn)
        {
            const string key = "trad-entry-";
            using var hasher = SHA256.Create();
            return key + Convert.ToBase64String(hasher.ComputeHash(Encoding.UTF8.GetBytes(ssn)));
        }

        public static string MaskSsn(string ssn)
        {
            return ssn[..6] + "*****";
        }
    }
}
