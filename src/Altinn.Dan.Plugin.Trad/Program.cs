using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Altinn.Dan.Plugin.Trad.Config;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
            var builder = Host.CreateDefaultBuilder();
            builder.ConfigureServices((context, services) =>
            {
            var config = context.Configuration;
            var connection = config.GetValue<string>("RedisConnectionString");
            var retry = config.GetValue<TimeSpan>("Breaker_RetryWaitTime");
            });
            builder.Build();


            var host = new HostBuilder()
                .ConfigureFunctionsWorkerDefaults()
                .ConfigureServices((context, services) =>
                {
                    services.AddLogging();
                    services.AddHttpClient();

                    services.AddSingleton<EvidenceSourceMetadata>();

                    services.Configure<ApplicationSettings>(context.Configuration.GetSection("ApplicationSettings"));

                    ApplicationSettings = new ApplicationSettings();
                    context.Configuration.GetSection("ApplicationSettings").Bind(ApplicationSettings);

                    // services.AddSingleton<IApplicationSettings, ApplicationSettings>();


                    services.AddStackExchangeRedisCache(option => { option.Configuration = ApplicationSettings.RedisConnectionString; });

                    var distributedCache = services.BuildServiceProvider().GetRequiredService<IDistributedCache>();
                    var registry = new PolicyRegistry()
                    {
                        { "defaultCircuitBreaker", HttpPolicyExtensions.HandleTransientHttpError().CircuitBreakerAsync(4, ApplicationSettings.Breaker_RetryWaitTime) },
                        { "CachePolicy", Policy.CacheAsync(distributedCache.AsAsyncCacheProvider<string>(), TimeSpan.FromHours(12)) }
                    };
                    services.AddPolicyRegistry(registry);

                    // Client configured with circuit breaker policies
                    services.AddHttpClient("SafeHttpClient", client => { client.Timeout = new TimeSpan(0, 0, 30); })
                        .AddPolicyHandlerFromRegistry("defaultCircuitBreaker");

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
