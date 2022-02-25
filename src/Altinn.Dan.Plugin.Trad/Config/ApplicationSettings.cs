using System;
using System.Threading.Tasks;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

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


        public KeyVaultClient keyVaultClient
        {
            get
            {
                AzureServiceTokenProvider azureServiceTokenProvider = new AzureServiceTokenProvider();
                return _keyVaultClient ??= new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback));
            }
        }

        private KeyVaultClient _keyVaultClient;
    }
}
