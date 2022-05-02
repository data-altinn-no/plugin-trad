using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Altinn.Dan.Plugin.Trad.Config;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Caching.Distributed;
using Polly.Extensions.Http;
using Polly.Registry;


namespace Altinn.Dan.Plugin.Trad
{
    class Program
    {
        private static IApplicationSettings ApplicationSettings { get; set; }

       
        private static Task Main(string[] args)
        {           
            var host = new HostBuilder()
                .ConfigureFunctionsWorkerDefaults()
                .ConfigureServices((context, services) =>
                {
                    services.AddLogging();
                    services.AddHttpClient();

                    services.AddSingleton<EvidenceSourceMetadata>();

                    services.AddOptions<ApplicationSettings>()
                        .Configure<IConfiguration>((settings, configuration) => configuration.Bind(settings));

                    ApplicationSettings = services.BuildServiceProvider().GetRequiredService<IOptions<ApplicationSettings>>().Value;

                    services.AddStackExchangeRedisCache(option =>
                    {
                        option.Configuration = ApplicationSettings.RedisConnectionString;
                    });

                    var distributedCache = services.BuildServiceProvider().GetRequiredService<IDistributedCache>();
                    var registry = new PolicyRegistry()
                    {
                        { "defaultCircuitBreaker", HttpPolicyExtensions.HandleTransientHttpError().CircuitBreakerAsync(4, ApplicationSettings.Breaker_RetryWaitTime) },
                        { "CachePolicy", Policy.CacheAsync(distributedCache.AsAsyncCacheProvider<string>(), TimeSpan.FromHours(12)) }
                    };
                    services.AddPolicyRegistry(registry);

                    // Client configured with circuit breaker policies

                    services.AddHttpClient("SafeHttpClient", client => {
                            client.Timeout = new TimeSpan(0, 0, 30);
                        })
                        .AddPolicyHandlerFromRegistry("defaultCircuitBreaker")
                        ;

                    // Client configured without circuit breaker policies. shorter timeout
                    services.AddHttpClient("CachedHttpClient", client => { client.Timeout = new TimeSpan(0, 0, 5); });

                    services.Configure<JsonSerializerOptions>(options =>
                    {
                        options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                        options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                        options.Converters.Add(new JsonStringEnumConverter());
                    });
                })
                .Build();
            return host.RunAsync();
        }
    }
}
