using System;
using System.Threading;
using L2Cache.Abstractions;
using BroadcastBusClient;

namespace BroadcastBus.ConsoleHarness
{
    internal sealed class HarnessApp : IDisposable
    {
        private readonly IBroadcastBus bus;
        private readonly string instance;
        private readonly BroadcastBusClientOptions opts;
        private readonly CancellationTokenSource cts = new();

        public HarnessApp(IBroadcastBus bus, string instance, BroadcastBusClientOptions opts)
        {
            this.bus = bus ?? throw new ArgumentNullException(nameof(bus));
            this.instance = string.IsNullOrWhiteSpace(instance) ? $"node-{Environment.ProcessId}" : instance;
            this.opts = opts ?? throw new ArgumentNullException(nameof(opts));

            bus.MessageReceived += OnMessage;
            Console.CancelKeyPress += OnCancelKeyPress;
            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
        }

        public void Run()
        {
            PrintHeader();

            while (!cts.IsCancellationRequested)
            {
                Console.Write("> ");
                var line = Console.ReadLine();
                if (line is null) break;        // EOF
                line = line.Trim();
                if (line.Length == 0) continue;

                if (line.Equals("/quit", StringComparison.OrdinalIgnoreCase)) break;
                if (line.Equals("/me",   StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine(
                        $"ME: {instance} @ {opts.NatsHostEndpointUrl} " +
                        $"(msg='{opts.MessageSubject}', noEcho={opts.NoEcho}, mTLS={opts.UseMtls})");
                    continue;
                }

                bus.PublishMessage(line);
                Console.WriteLine($"[TX] {instance} => "{line}"");
            }
        }

        private void OnMessage(string text) => Console.WriteLine($"[RX] {instance} <= "{text}"");

        private void PrintHeader()
        {
            Console.WriteLine("=== BroadcastBus Console Harness (Messaging) ===");
            Console.WriteLine($"Instance : {instance}");
            Console.WriteLine($"NATS URL : {opts.NatsHostEndpointUrl}");
            Console.WriteLine($"MessageS : '{opts.MessageSubject}'");
            Console.WriteLine($"NoEcho   : {opts.NoEcho}");
            Console.WriteLine($"UseMtls  : {opts.UseMtls}");
            Console.WriteLine("Commands: <text> to send | /me | /quit\n");
        }

        private void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs e) { e.Cancel = true; cts.Cancel(); }
        private void OnProcessExit(object? sender, EventArgs e) => cts.Cancel();

        public void Dispose()
        {
            bus.MessageReceived -= OnMessage;
            Console.CancelKeyPress -= OnCancelKeyPress;
            AppDomain.CurrentDomain.ProcessExit -= OnProcessExit;
            cts.Cancel();
            cts.Dispose();
        }
    }
}