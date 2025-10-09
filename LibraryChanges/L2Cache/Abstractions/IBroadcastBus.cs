using System;
namespace L2Cache.Abstractions
{
    public interface IBroadcastBus : IDisposable
    {
        void Initialize();
        void PublishEviction(ReadOnlyMemory<byte> payload);
        void PublishClearAll();
        void PublishMessage(string message);
        event Action<ReadOnlyMemory<byte>>? EvictionReceived;
        event EventHandler? ClearAllReceived;
        event Action<string>? MessageReceived;
    }
}