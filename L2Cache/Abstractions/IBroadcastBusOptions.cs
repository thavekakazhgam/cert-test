namespace L2Cache.Abstractions
{
    /// <summary>Options for BroadcastBus.</summary>
    public interface IBroadcastBusOptions
    {
        string NatsHostEndpointUrl { get; set; }
        string EvictSubject { get; set; }
        string ClearAllSubject { get; set; }
        string? Username { get; set; }
        string? Password { get; set; }
        bool NoEcho { get; set; }

        // TLS/mTLS
        bool UseMtls { get; set; }
        bool UseWindowsStoreClientCert { get; set; }
        string? WindowsStoreSubjectCN { get; set; }

        // Harness chat (optional)
        string? MessageSubject { get; set; }

        // Which channels to start
        BroadcastBusMode Mode { get; set; }
    }
}