
# BroadcastBus – Main App & Harness (Everything in One Place)

This package includes:
- **Library changes** (Mode + UseMtls + Messaging API + DI variants inside the BroadcastBusClient package)
- **Main app** DI example (`ServiceProvider.cs`)
- **Harness console app** (Program + HarnessApp + appsettings.json)

## How to use

### 1) Apply library changes
Copy files from `LibraryChanges/BroadcastBusClient_Embedded/BroadcastBusClient_Embedded/...` into your solution.
Rebuild **BroadcastBusClient** (and interface assembly).

### 2) Update your main app DI
Use `MainApp_Changes/Framework/ServiceProvider.cs` as reference.
- Call `services.AddBroadcastBusClientApp(opts => { ... });`
- No event wiring here—the package wires L2Cache eviction/clear handlers internally.

### 3) Harness app
Open `HarnessApp/BroadcastBus.ConsoleHarness`:
- Add **Dependencies** to your compiled DLLs:
  - `BroadcastBusClient.dll`
  - the assembly with `IBroadcastBus` (your current `L2Cache.dll` or a new `BroadcastBus.Abstractions.dll`)
- Edit `appsettings.json` if needed (URL, subject, NoEcho, UseMtls).
- Run two or more instances with a NATS server running.

#### Run
```
nats-server -p 4222
dotnet run --project HarnessApp/BroadcastBus.ConsoleHarness
```

Type in one window—appears in the others. Commands: `/me`, `/quit`.

Enjoy!
