using System;
using System.Threading.Tasks;

namespace Altinn.Dan.Plugin.Trad.Config
{
    public class ApplicationSettings : IApplicationSettings
    {

        public string RedisConnectionString { get; set; }
        public bool IsTest { get; set; }

        public TimeSpan Breaker_RetryWaitTime { get; set; }

        public TimeSpan Breaker_OpenCircuitTime { get; set; }

        public string RegistryURL { get; set; }

        public string KeyVaultName { get; set; }
        public string KeyVaultClientId { get; set; }
        public string KeyVaultClientSecret { get; set; }
        public string KeyVaultSslCertificate { get; set; }
        public string ApiKey { get; set; }
    }
}
