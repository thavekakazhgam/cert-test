using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using BroadcastBusClient;

namespace MSM.MAPS.Infrastructure
{
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
            // AddL2Cache(...) remains in your project (omitted here)

            services.AddBroadcastBusClientApp(opts =>
            {
                var tenant = configuration["TenantId"] ?? "default-tenant";
                opts.NatsHostEndpointUrl = configuration["Nats:Url"];
                opts.EvictSubject        = $"{tenant}.cache.evict";
                opts.ClearAllSubject     = $"{tenant}.cache.clearAll";
                opts.NoEcho              = true;

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