using System;

namespace Altinn.Dan.Plugin.Trad.Config
{
    public class ApplicationSettings : IApplicationSettings
    {
 
        public string RedisConnectionString { get; set; }
        public bool IsTest { get; set; }

        public TimeSpan Breaker_RetryWaitTime { get; set; }

        public TimeSpan Breaker_OpenCircuitTime { get; set; }

        public string PersonURL { get; set; }

        public string CompanyURL { get; set; }
    }
}
