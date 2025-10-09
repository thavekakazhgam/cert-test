using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using BroadcastBusClient;

namespace MSM.MAPS.Infrastructure
{
    /// <summary>Minimal framework ServiceProvider showing clean DI registration for the bus.</summary>
    public abstract class ServiceProvider<T> where T : class
    {
        private IServiceProvider provider;
        private readonly IHostBuilder hostBuilder;
        private static ServiceProvider<T> instance;

        public static ServiceProvider<T> Instance => instance ?? throw new Exception("Initialise first.");

        protected ServiceProvider()
        {
            hostBuilder = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) => AddServices(services, context.Configuration));
        }

        protected virtual void AddServices(IServiceCollection services, IConfiguration configuration)
        {
            // Your existing AddL2Cache(...) here as-is.

            // Clean BroadcastBus registration: all details encapsulated in the package.
            services.AddBroadcastBusClientApp(opts =>
            {
                var tenant = configuration["TenantId"] ?? "default-tenant";
                opts.NatsHostEndpointUrl = configuration["Nats:Url"];
                opts.EvictSubject        = $"{tenant}.cache.evict";
                opts.ClearAllSubject     = $"{tenant}.cache.clearAll";
                opts.NoEcho              = true;

                // TLS/mTLS and auth
                bool.TryParse(configuration["Nats:UseMtls"], out var useMtls);
                opts.UseMtls                   = useMtls;
                opts.UseWindowsStoreClientCert = useMtls;
                opts.WindowsStoreSubjectCN     = configuration["Nats:CertificateCN"];
                opts.Username                  = configuration["Nats:Username"];
                opts.Password                  = configuration["Nats:Password"];
            });
        }

        public ServiceProvider<T> Build()
        {
            provider = hostBuilder.Build().Services;
            instance = this;
            return this;
        }

        public object GetService(Type t) => provider.GetService(t);
    }
}