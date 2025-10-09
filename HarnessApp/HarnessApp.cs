using System;
using System.Threading;
using BroadcastBus.ConsoleHarness.Configuration;
using L2Cache.Abstractions;

namespace BroadcastBus.ConsoleHarness
{
    internal sealed class HarnessApp : IDisposable
    {
        private readonly IBroadcastBus bus;
        private readonly BusSettings settings;
        private readonly CancellationTokenSource cts = new();

        public HarnessApp(IBroadcastBus bus, BusSettings settings)
        {
            this.bus = bus ?? throw new ArgumentNullException(nameof(bus));
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));

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
                        $"ME: {settings.Instance} @ {settings.NatsHostEndpointUrl} " +
                        $"(msg='{settings.MessageSubject}', noEcho={settings.NoEcho}, mTLS={settings.UseMtls})");
                    continue;
                }

                bus.PublishMessage(line);
                Console.WriteLine($"[TX] {settings.Instance} => "{line}"");
            }
        }

        private void OnMessage(string text)
        {
            Console.WriteLine($"[RX] {settings.Instance} <= "{text}"");
        }

        private void PrintHeader()
        {
            Console.WriteLine("=== BroadcastBus Console Harness (Messaging) ===");
            Console.WriteLine($"Instance : {settings.Instance}");
            Console.WriteLine($"NATS URL : {settings.NatsHostEndpointUrl}");
            Console.WriteLine($"MessageS : '{settings.MessageSubject}'");
            Console.WriteLine($"NoEcho   : {settings.NoEcho}");
            Console.WriteLine($"UseMtls  : {settings.UseMtls}");
            Console.WriteLine("Commands: <text> to send | /me | /quit\n");
        }

        private void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true;
            cts.Cancel();
        }

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