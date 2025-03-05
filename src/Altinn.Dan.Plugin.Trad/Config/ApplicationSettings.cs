using System;

namespace Altinn.Dan.Plugin.Trad.Config
{
    public class ApplicationSettings : IApplicationSettings
    {
        public const string RedisBulkEntryKey = "advreg_bulk";
        public const string RedisBulkEntryPrivateKey = "advregprivate_bulk";
        public const string ZipEntryFileName = "advreg.json";

        public string RedisConnectionString { get; set; }

        public int BreakerFailuresBeforeTripping { get; set; }

        public TimeSpan BreakerOpenCircuitTime { get; set; }

        public string RegistryURL { get; set; }

        public string ApiKey { get; set; }
    }
}
