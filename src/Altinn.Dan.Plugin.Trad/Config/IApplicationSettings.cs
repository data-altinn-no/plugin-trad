using System;

namespace Altinn.Dan.Plugin.Trad.Config
{
    public interface IApplicationSettings
    {
        string RedisConnectionString { get; }
        TimeSpan Breaker_RetryWaitTime { get; }
        TimeSpan Breaker_OpenCircuitTime { get; }
        bool IsTest { get; }
        string PersonURL { get; }

        string CompanyURL { get; }
    }
}
