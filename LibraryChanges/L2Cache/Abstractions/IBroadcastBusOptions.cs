namespace L2Cache.Abstractions
{
    public interface IBroadcastBusOptions
    {
        string NatsHostEndpointUrl { get; set; }
        string EvictSubject { get; set; }
        string ClearAllSubject { get; set; }
        string? Username { get; set; }
        string? Password { get; set; }
        bool NoEcho { get; set; }

        bool UseMtls { get; set; }
        bool UseWindowsStoreClientCert { get; set; }
        string? WindowsStoreSubjectCN { get; set; }

        string? MessageSubject { get; set; }
        BroadcastBusMode Mode { get; set; }
    }
}