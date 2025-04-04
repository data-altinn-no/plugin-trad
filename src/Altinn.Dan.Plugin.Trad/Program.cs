using Altinn.Dan.Plugin.Trad.Config;
using Altinn.Dan.Plugin.Trad.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Dan.Common.Extensions;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Altinn.ApiClients.Maskinporten.Interfaces;
using Altinn.ApiClients.Maskinporten.Services;
using Altinn.ApiClients.Maskinporten.Config;
using Altinn.ApiClients.Maskinporten.Extensions;
using System;

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

        services.AddSingleton<IMaskinportenService, MaskinportenService>();
        services.AddMemoryCache();
        services.AddSingleton<ITokenCacheProvider, MemoryTokenCacheProvider>();

        services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(applicationSettings.RedisConnectionString));

        services.AddTransient<IOrganizationService, OrganizationService>();


        var maskinportenSettings = new MaskinportenSettings()
        {
            EncodedX509 = applicationSettings.Certificate,
            ClientId = applicationSettings.ClientId,
            Scope = applicationSettings.Scope,
            Environment = applicationSettings.MaskinportenEnv
        };

        services.AddMaskinportenHttpClient<SettingsX509ClientDefinition>("myMaskinportenClient", maskinportenSettings);
    })
    .Build();

await host.RunAsync();
