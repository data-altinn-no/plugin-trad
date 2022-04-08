using System;
using System.Threading.Tasks;
using Azure.Security.KeyVault.Secrets;
using Azure.Security.KeyVault.Certificates;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Azure.Identity;

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
        public string ApiKeySecret { get; set; }


        public SecretClient secretClient
        {
            get
            {
                AzureServiceTokenProvider azureServiceTokenProvider = new AzureServiceTokenProvider();
                return _secretClient ??= new SecretClient(new Uri($"https://{KeyVaultName}.vault.azure.net/"), new DefaultAzureCredential());
            }
        }

        public CertificateClient certificateClient
        {
            get
            {
                return _certificateClient ??= new CertificateClient(new Uri($"https://{KeyVaultName}.vault.azure.net/"), new DefaultAzureCredential());
            }
        }

        private SecretClient _secretClient;
        private CertificateClient _certificateClient;
    }
}
