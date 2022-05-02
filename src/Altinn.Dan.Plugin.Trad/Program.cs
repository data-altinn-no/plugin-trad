using Altinn.Dan.Plugin.Trad.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;
using Polly.Registry;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Nadobe.Common.Interfaces;


namespace Altinn.Dan.Plugin.Trad
{
    class Program
    {
        private static IApplicationSettings ApplicationSettings { get; set; }

        // ReSharper disable once UnusedParameter.Local
        private static Task Main(string[] args)
        {           
            var host = new HostBuilder()
                .ConfigureFunctionsWorkerDefaults()
                .ConfigureServices((_, services) =>
                {
                    services.AddLogging();
                    services.AddHttpClient();

                    services.AddSingleton<IEvidenceSourceMetadata, EvidenceSourceMetadata>();

                    services.AddOptions<ApplicationSettings>()
                        .Configure<IConfiguration>((settings, configuration) => configuration.Bind(settings));

                    ApplicationSettings = services.BuildServiceProvider().GetRequiredService<IOptions<ApplicationSettings>>().Value;

                    services.AddStackExchangeRedisCache(option =>
                    {
                        option.Configuration = ApplicationSettings.RedisConnectionString;
                    });

                    var registry = new PolicyRegistry()
                    {
                        { "defaultCircuitBreaker", HttpPolicyExtensions.HandleTransientHttpError().CircuitBreakerAsync(
                            ApplicationSettings.BreakerFailuresBeforeTripping, ApplicationSettings.BreakerOpenCircuitTime) }
                    };
                    services.AddPolicyRegistry(registry);

                    // Client configured with circuit breaker policies
                    services.AddHttpClient("SafeHttpClient", client => {
                            client.Timeout = new TimeSpan(0, 0, 30);
                        })
                        .AddPolicyHandlerFromRegistry("defaultCircuitBreaker")
                        ;

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
