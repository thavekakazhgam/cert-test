namespace BroadcastBus.ConsoleHarness.Configuration
{
    public sealed class BusSettings
    {
        public string Instance { get; set; } = $"node-{System.Environment.ProcessId}";
        public string NatsHostEndpointUrl { get; set; } = "nats://127.0.0.1:4222";
        public string MessageSubject { get; set; } = "pentonic.console.message";
        public bool   NoEcho { get; set; } = true;

        public bool   UseMtls { get; set; } = false;
        public bool   UseWindowsStoreClientCert { get; set; } = false;
        public string? WindowsStoreSubjectCN { get; set; }

        public string? Username { get; set; }
        public string? Password { get; set; }
    }
}