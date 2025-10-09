using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using L2Cache.Abstractions;   // IBroadcastBus
using BroadcastBusClient;     // BroadcastBusClientOptions, AddBroadcastBusClientMessaging(...)

namespace BroadcastBus.ConsoleHarness
{
    internal static class Program
    {
        public static void Main(string[] args)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            var busOpts = new BroadcastBusClientOptions();
            var section = config.GetSection("BroadcastBus");
            if (section.Exists()) section.Bind(busOpts); else config.Bind(busOpts);

            var instance = config["Instance"] ?? $"node-{Environment.ProcessId}";
            using var sp = BuildServiceProvider(busOpts);
            var bus = sp.GetRequiredService<IBroadcastBus>();

            using var app = new HarnessApp(bus, instance, busOpts);
            app.Run();
        }

        private static ServiceProvider BuildServiceProvider(BroadcastBusClientOptions busOpts)
        {
            var services = new ServiceCollection();
            services.AddBroadcastBusClientMessaging(opts =>
            {
                opts.NatsHostEndpointUrl       = busOpts.NatsHostEndpointUrl;
                opts.MessageSubject            = busOpts.MessageSubject;
                opts.NoEcho                    = busOpts.NoEcho;
                opts.UseMtls                   = busOpts.UseMtls;
                opts.UseWindowsStoreClientCert = busOpts.UseWindowsStoreClientCert;
                opts.WindowsStoreSubjectCN     = busOpts.WindowsStoreSubjectCN;
                opts.Username                  = busOpts.Username;
                opts.Password                  = busOpts.Password;
            });
            return services.BuildServiceProvider();
        }
    }
}