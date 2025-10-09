using System.Text;
using NATS.Client.Core;
using L2Cache.Abstractions;
namespace BroadcastBusClient
{
    public sealed class BroadcastBus : IBroadcastBus
    {
        private readonly IBroadcastBusOptions options;
        private bool disposed;
        private bool initialized;
        private NatsConnection? connection;
        private CancellationTokenSource? cts;

        public BroadcastBus(IBroadcastBusOptions options) => this.options = options ?? throw new ArgumentNullException(nameof(options));

        public event Action<ReadOnlyMemory<byte>>? EvictionReceived;
        public event EventHandler? ClearAllReceived;
        public event Action<string>? MessageReceived;

        public void Initialize()
        {
            if (initialized) return;
            ThrowIfDisposed();
            cts = new CancellationTokenSource();
            var natsOpts = CreateNatsOptions();
            connection = new NatsConnection(natsOpts);

            if (options.Mode == BroadcastBusMode.Cache || options.Mode == BroadcastBusMode.Both)
            {
                _ = StartEvictSubscription(cts.Token);
                _ = StartClearAllSubscription(cts.Token);
            }
            if ((options.Mode == BroadcastBusMode.Messaging || options.Mode == BroadcastBusMode.Both) &&
                !string.IsNullOrWhiteSpace(options.MessageSubject))
            {
                _ = StartMessageSubscription(cts.Token);
            }
            initialized = true;
        }

        public void PublishEviction(ReadOnlyMemory<byte> payload)
        {
            ThrowIfNotInitialized(); ThrowIfDisposed();
            connection!.PublishAsync(options.EvictSubject, payload).GetAwaiter().GetResult();
        }
        public void PublishClearAll()
        {
            ThrowIfNotInitialized(); ThrowIfDisposed();
            connection!.PublishAsync(options.ClearAllSubject, ReadOnlyMemory<byte>.Empty).GetAwaiter().GetResult();
        }
        public void PublishMessage(string message)
        {
            ThrowIfNotInitialized(); ThrowIfDisposed();
            if (string.IsNullOrWhiteSpace(options.MessageSubject))
                throw new InvalidOperationException("MessageSubject is not configured.");
            var bytes = Encoding.UTF8.GetBytes(message ?? string.Empty);
            connection!.PublishAsync(options.MessageSubject, bytes).GetAwaiter().GetResult();
        }

        private async Task StartEvictSubscription(CancellationToken ct)
        {
            await foreach (var msg in connection!.SubscribeAsync<ReadOnlyMemory<byte>>(options.EvictSubject, cancellationToken: ct))
            {
                try { EvictionReceived?.Invoke(msg.Data); } catch { }
            }
        }
        private async Task StartClearAllSubscription(CancellationToken ct)
        {
            await foreach (var msg in connection!.SubscribeAsync<ReadOnlyMemory<byte>>(options.ClearAllSubject, cancellationToken: ct))
            {
                try { ClearAllReceived?.Invoke(this, EventArgs.Empty); } catch { }
            }
        }
        private async Task StartMessageSubscription(CancellationToken ct)
        {
            var subject = options.MessageSubject!;
            await foreach (var msg in connection!.SubscribeAsync<ReadOnlyMemory<byte>>(subject, cancellationToken: ct))
            {
                try { MessageReceived?.Invoke(Encoding.UTF8.GetString(msg.Data.Span)); } catch { }
            }
        }

        private NatsOpts CreateNatsOptions()
        {
            var url = options.NatsHostEndpointUrl ?? "nats://127.0.0.1:4222";
            if (options.UseMtls && url.StartsWith("nats://", StringComparison.OrdinalIgnoreCase))
                url = "nats+tls://" + url["nats://".Length..];
            var builder = NatsOpts.Default.WithUrl(url).WithNoEcho(options.NoEcho);
            if (!string.IsNullOrWhiteSpace(options.Username))
                builder = builder.WithUserAndPassword(options.Username!, options.Password ?? string.Empty);
            return builder;
        }

        private void ThrowIfDisposed() { if (disposed) throw new ObjectDisposedException(nameof(BroadcastBus)); }
        private void ThrowIfNotInitialized() { if (!initialized) throw new InvalidOperationException("Call Initialize() first."); }
        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            cts?.Cancel();
            cts?.Dispose();
            connection?.DisposeAsync();
        }
    }
}