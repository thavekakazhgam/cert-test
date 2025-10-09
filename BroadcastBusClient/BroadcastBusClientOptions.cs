using L2Cache.Abstractions;

namespace BroadcastBusClient
{
    /// <summary>Concrete options used by the client.</summary>
    public class BroadcastBusClientOptions : IBroadcastBusOptions
    {
        public string NatsHostEndpointUrl { get; set; } = "nats://127.0.0.1:4222";
        public string EvictSubject { get; set; } = "cache.evict";
        public string ClearAllSubject { get; set; } = "cache.clearAll";
        public string? Username { get; set; }
        public string? Password { get; set; }
        public bool NoEcho { get; set; } = true;

        public bool UseMtls { get; set; } = false;
        public bool UseWindowsStoreClientCert { get; set; } = false;
        public string? WindowsStoreSubjectCN { get; set; }

        public string? MessageSubject { get; set; } = "pentonic.console.message";
        public BroadcastBusMode Mode { get; set; } = BroadcastBusMode.Cache;
    }
}