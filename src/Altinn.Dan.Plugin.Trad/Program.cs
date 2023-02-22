using Altinn.Dan.Plugin.Trad.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Dan.Common.Extensions;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

var host = new HostBuilder()
    .ConfigureDanPluginDefaults()
    .ConfigureLogging(loggingConfiguration =>
    {
        loggingConfiguration.AddConsole();
    })
    .ConfigureServices((_, services) =>
    {
        services.AddOptions<ApplicationSettings>()
            .Configure<IConfiguration>((settings, configuration) => configuration.Bind(settings));

        var applicationSettings = services.BuildServiceProvider().GetRequiredService<IOptions<ApplicationSettings>>().Value;

        services.AddStackExchangeRedisCache(option =>
        {
            option.Configuration = applicationSettings.RedisConnectionString;
        });

        services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(applicationSettings.RedisConnectionString));

    })
    .Build();

await host.RunAsync();
