using System;

namespace L2Cache.Abstractions
{
    /// <summary>Transport-agnostic bus API used by L2Cache and the messaging harness.</summary>
    public interface IBroadcastBus : IDisposable
    {
        // Lifecycle
        void Initialize();

        // L2Cache publishes
        void PublishEviction(ReadOnlyMemory<byte> payload);
        void PublishClearAll();

        // Messaging publish (for harness)
        void PublishMessage(string message);

        // Events
        event Action<ReadOnlyMemory<byte>>? EvictionReceived;
        event EventHandler? ClearAllReceived;
        event Action<string>? MessageReceived; // for MessageSubject
    }
}