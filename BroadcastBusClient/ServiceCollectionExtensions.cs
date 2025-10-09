using System;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using L2Cache.Abstractions;
using L2Cache.Data;
using L2Cache.Invalidation;

namespace BroadcastBusClient
{
    /// <summary>DI entry points kept inside the broadcast bus package.</summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Application (L2Cache) variant: Mode=Cache, wires eviction/clear handlers, then Initialize().
        /// </summary>
        public static IServiceCollection AddBroadcastBusClientApp(this IServiceCollection services, Action<BroadcastBusClientOptions> configure)
        {
            if (configure is null) throw new ArgumentNullException(nameof(configure));

            services.TryAddSingleton<IBroadcastBusClientFactory, BroadcastBusClientFactory>();
            services.TryAddSingleton<IInvalidationKeySetCodec, SimpleJsonInvalidationKeySetCodec>();

            var opts = new BroadcastBusClientOptions { Mode = BroadcastBusMode.Cache };
            configure(opts);
            services.TryAddSingleton<IBroadcastBusOptions>(opts);

            services.TryAddSingleton<IBroadcastBus>(provider =>
            {
                var factory = provider.GetRequiredService<IBroadcastBusClientFactory>();
                var bus = factory.Create(opts);

                var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();

                void OnEvict(ReadOnlyMemory<byte> payload)
                {
                    using var scope = scopeFactory.CreateScope();
                    var codec = scope.ServiceProvider.GetRequiredService<IInvalidationKeySetCodec>();
                    var cache = scope.ServiceProvider.GetRequiredService<ICacheServiceProvider>();

                    var json = Encoding.UTF8.GetString(payload.Span);
                    var keys = codec.Decode(json);

                    cache.EvictInstance(keys.InstanceKey);
                    if (keys.RootSetKeys?.Length > 0)   cache.EvictSets(keys.RootSetKeys);
                    if (keys.CrossLinkKeys?.Length > 0) cache.EvictCrossLinks(keys.CrossLinkKeys);
                }

                void OnClearAll(object? _, EventArgs __)
                {
                    using var scope = scopeFactory.CreateScope();
                    var cache = scope.ServiceProvider.GetRequiredService<ICacheServiceProvider>();
                    cache.ClearAllCachedEntries();
                }

                bus.EvictionReceived -= OnEvict;    // defensive single-subscription
                bus.EvictionReceived += OnEvict;
                bus.ClearAllReceived -= OnClearAll;
                bus.ClearAllReceived += OnClearAll;

                bus.Initialize();
                return bus;
            });

            return services;
        }

        /// <summary>
        /// Messaging variant: Mode=Messaging; no L2Cache wiring; Initialize() starts only message subject.
        /// </summary>
        public static IServiceCollection AddBroadcastBusClientMessaging(this IServiceCollection services, Action<BroadcastBusClientOptions>? configure = null)
        {
            services.TryAddSingleton<IBroadcastBusClientFactory, BroadcastBusClientFactory>();

            var opts = new BroadcastBusClientOptions { Mode = BroadcastBusMode.Messaging };
            if (string.IsNullOrWhiteSpace(opts.MessageSubject))
                opts.MessageSubject = "pentonic.console.message";

            configure?.Invoke(opts);
            services.TryAddSingleton<IBroadcastBusOptions>(opts);

            services.TryAddSingleton<IBroadcastBus>(provider =>
            {
                var factory = provider.GetRequiredService<IBroadcastBusClientFactory>();
                var bus = factory.Create(opts);
                bus.Initialize();    // starts only message subscription per mode
                return bus;
            });

            return services;
        }
    }
}