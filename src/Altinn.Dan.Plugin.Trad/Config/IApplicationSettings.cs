using System;

namespace Altinn.Dan.Plugin.Trad.Config
{
    public interface IApplicationSettings
    {
        string RedisConnectionString { get; set; }

        int BreakerFailuresBeforeTripping { get; set; }

        TimeSpan BreakerOpenCircuitTime { get; set; }

        string RegistryURL { get; set; }

        string ApiKey { get; set; }
    }
}
