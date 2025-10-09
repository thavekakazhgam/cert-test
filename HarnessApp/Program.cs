using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using BroadcastBus.ConsoleHarness.Configuration;
using L2Cache.Abstractions;     // IBroadcastBus
using BroadcastBusClient;       // AddBroadcastBusClientMessaging(...)

namespace BroadcastBus.ConsoleHarness
{
    internal static class Program
    {
        public static void Main(string[] args)
        {
            var config   = BuildConfiguration();
            var settings = LoadSettings(config);

            using var sp  = BuildServiceProvider(settings);
            var bus       = sp.GetRequiredService<IBroadcastBus>();

            using var app = new HarnessApp(bus, settings);
            app.Run();
        }

        private static IConfiguration BuildConfiguration() =>
            new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

        private static BusSettings LoadSettings(IConfiguration config) =>
            config.Get<BusSettings>() ?? new BusSettings();

        private static ServiceProvider BuildServiceProvider(BusSettings settings)
        {
            var services = new ServiceCollection();

            // Use the Messaging variant from your BroadcastBusClient DLL
            services.AddBroadcastBusClientMessaging(opts =>
            {
                opts.NatsHostEndpointUrl       = settings.NatsHostEndpointUrl;
                opts.MessageSubject            = settings.MessageSubject;
                opts.NoEcho                    = settings.NoEcho;
                opts.UseMtls                   = settings.UseMtls;
                opts.UseWindowsStoreClientCert = settings.UseWindowsStoreClientCert;
                opts.WindowsStoreSubjectCN     = settings.WindowsStoreSubjectCN;
                opts.Username                  = settings.Username;
                opts.Password                  = settings.Password;
            });

            return services.BuildServiceProvider();
        }
    }
}