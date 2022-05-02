using System;

namespace Altinn.Dan.Plugin.Trad.Config
{
    public class ApplicationSettings : IApplicationSettings
    {
        public string RedisConnectionString { get; set; }

        public int BreakerFailuresBeforeTripping { get; set; }

        public TimeSpan BreakerOpenCircuitTime { get; set; }

        public string RegistryURL { get; set; }

        public string ApiKey { get; set; }
    }
}
