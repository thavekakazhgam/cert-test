using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using BroadcastBusClient;
// If these live in your solution as shown earlier:
using L2Cache.Abstractions;
using L2Cache.Invalidation;

namespace MSM.MAPS.Infrastructure
{
    /// <summary>
    /// App/framework-level DI bootstrap. Kept generic — no BroadcastBus specifics here.
    /// </summary>
    /// <typeparam name="T">Runtime model type</typeparam>
    public abstract class ServiceProvider<T> where T : class
    {
        private IServiceProvider provider = default!;
        private readonly IHostBuilder hostBuilder;

        private static ServiceProvider<T>? instance;
        public static ServiceProvider<T> Instance =>
            instance ?? throw new Exception("A service provider has not been initialised. Call Build() first.");

        protected ServiceProvider()
        {
            hostBuilder = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) => AddServices(services, context.Configuration));
        }

        /// <summary>Resolve a service from the built provider.</summary>
        public object? GetService(Type serviceType) => provider.GetService(serviceType);

        /// <summary>
        /// Register framework-level services only.
        /// - L2Cache registration (your existing pattern)
        /// - BroadcastBus registration via single one-liner (details live in the package)
        /// </summary>
        protected virtual void AddServices(IServiceCollection services, IConfiguration configuration)
        {
            // L2Cache (unchanged)
            var cacheConfigurationSection = new CacheConfigurationSection();
            var options = new CacheConfigurationSectionOptions();
            options.ApplyAll(cacheConfigurationSection);
            services.AddL2Cache(cacheOptions => options.ApplyTo(cacheOptions));

            // BroadcastBus: one-liner; options are hardcoded inside the extension for now
            services.AddBroadcastBusClient();
        }

        public ServiceProvider<T> Build()
        {
            provider = hostBuilder.Build().Services;

            // Optional: eagerly instantiate the bus so subscriptions/handlers are "hot" right after build
            _ = provider.GetService(typeof(IBroadcastBus));

            instance = this;
            return this;
        }

        public ServiceProvider<T> WithRuntimeModel(T runtimeModel)
        {
            AddRuntimeModel(runtimeModel);
            return this;
        }

        private void AddRuntimeModel(T runtimeModel)
        {
            hostBuilder.ConfigureServices(services =>
                services.TryAddSingleton<T>(_ => runtimeModel));
        }
    }
}
