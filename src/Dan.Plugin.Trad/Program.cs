using Microsoft.Extensions.Hosting;
using Dan.Common.Extensions;
using Dan.Plugin.Trad;
using Microsoft.Extensions.DependencyInjection;

var host = new HostBuilder()
    .ConfigureDanPluginDefaults()
    .ConfigureAppConfiguration((context, configuration) =>
    {
        // Add more configuration sources if necessary. ConfigureDanPluginDefaults will load environment variables, which includes
        // local.settings.json (if developing locally) and applications settings for the Azure Function
    })
    .ConfigureServices((context, services) =>
    {
        // Add any additional services here

        // This makes IOption<Settings> available in the DI container.
        var configurationRoot = context.Configuration;
        services.Configure<Settings>(
            configurationRoot.GetSection(nameof(Settings)));


        services.AddStackExchangeRedisCache(option =>
        {
            option.Configuration = context.Configuration["Settings:RedisConnectionString"];
        });

    })
    .Build();

await host.RunAsync();

