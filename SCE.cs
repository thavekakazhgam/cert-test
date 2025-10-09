using System;
using System.Text;
using L2Cache.Abstractions;
using L2Cache.Data;
using L2Cache.Invalidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BroadcastBusClient
{
    /// <summary>
    /// DI extensions for BroadcastBusClient.
    /// Single-variant: AddBroadcastBusClient() with hardcoded options for now.
    /// Event handlers are attached BEFORE Initialize() to avoid any race window.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers BroadcastBus as a singleton, wires inbound handlers, then initializes the connection.
        /// Options are temporarily hardcoded here (replace later with SettingsManager/config).
        /// </summary>
        public static IServiceCollection AddBroadcastBusClient(this IServiceCollection services)
        {
            // Ensure required dependencies
            services.TryAddSingleton<IBroadcastBusClientFactory, BroadcastBusClientFactory>();
            services.TryAddSingleton<IInvalidationKeySetCodec, SimpleJsonInvalidationKeySetCodec>();

            // The bus singleton
            services.TryAddSingleton<IBroadcastBus>(provider =>
            {
                // --- Hardcoded options (swap out later) ---
                var options = new BroadcastBusClientOptions
                {
                    NatsHostEndpointUrl     = "nats://127.0.0.1:4222",
                    EvictSubject            = "tenantA.cache.evict",
                    ClearAllSubject         = "tenantA.cache.clearAll",
                    NoEcho                  = true,
                    Username                = null,
                    Password                = null,
                    UseWindowsStoreClientCert = false,
                    WindowsStoreSubjectCN     = null
                };
                // ------------------------------------------

                var factory = provider.GetRequiredService<IBroadcastBusClientFactory>();
                var bus     = factory.Create(options);

                // We need a fresh scope per inbound message (tenant isolation / lifetimes)
                var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();

                // Define handlers (attach BEFORE Initialize)
                void OnEvict(ReadOnlyMemory<byte> payload)
                {
                    using var scope = scopeFactory.CreateScope();
                    var sp    = scope.ServiceProvider;
                    var codec = sp.GetRequiredService<IInvalidationKeySetCodec>();
                    var cache = sp.GetRequiredService<ICacheServiceProvider>();

                    var json = Encoding.UTF8.GetString(payload.Span);
                    var keys = codec.Decode(json);

                    // Local-only eviction (never republish here)
                    cache.EvictInstance(keys.InstanceKey);
                    if (keys.RootSetKeys?.Length  > 0) cache.EvictSets(keys.RootSetKeys);
                    if (keys.CrossLinkKeys?.Length > 0) cache.EvictCrossLinks(keys.CrossLinkKeys);
                }

                void OnClearAll(object? _, EventArgs __)
                {
                    using var scope = scopeFactory.CreateScope();
                    var cache = scope.ServiceProvider.GetRequiredService<ICacheServiceProvider>();
                    cache.ClearAllCachedEntries();
                }

                // Exactly-once wiring (defensive)
                bus.EvictionReceived -= OnEvict;
                bus.EvictionReceived += OnEvict;
                bus.ClearAllReceived -= OnClearAll;
                bus.ClearAllReceived += OnClearAll;

                // Now open the connection and start the two subscriptions
                bus.Initialize();

                return bus;
            });

            return services;
        }
    }
}
