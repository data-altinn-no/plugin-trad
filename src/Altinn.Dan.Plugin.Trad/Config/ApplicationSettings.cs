using System;
using System.Security.Cryptography.X509Certificates;

namespace Altinn.Dan.Plugin.Trad.Config
{
    public class ApplicationSettings : IApplicationSettings
    {
        public const string RedisBulkEntryKey = "advreg_bulk";
        public const string RedisBulkEntryPrivateKey = "advregprivate_bulk";
        public const string RedisRegNumberListKey = "advreg_regnrlist";
        public const string ZipEntryFileName = "advreg.json";

        private string _cert;

        public string RedisConnectionString { get; set; }

        public int BreakerFailuresBeforeTripping { get; set; }

        public TimeSpan BreakerOpenCircuitTime { get; set; }

        public string RegistryURL { get; set; }

        public string ApiKey { get; set; }

        public string KeyVaultName { get; set; }

        public string CertName { get; set; }

        public string MaskinportenEnv { get; set; }

        public string ClientId { get; set; }

        public string Scope { get; set; }

        public string Certificate
        {
            get
            {
                return _cert ?? new PluginKeyVault(KeyVaultName).GetCertificateAsBase64(CertName).Result;
            }
            set
            {
                _cert = value;
            }
        }
    }
}
