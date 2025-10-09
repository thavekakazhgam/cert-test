# BroadcastBus – Main App & Harness (v2: single source of truth = appsettings.json)

**What changed**: The harness now binds **directly** into `BroadcastBusClientOptions` from `appsettings.json`.  
No separate `BusSettings` class—edit only `appsettings.json`.

## Layout
- `LibraryChanges/` — Mode + UseMtls + Messaging API + DI variants
- `MainApp_Changes/Framework/ServiceProvider.cs` — clean DI sample (app variant)
- `HarnessApp/BroadcastBus.ConsoleHarness` — minimal console harness

## Steps
1) Copy `LibraryChanges/...` into your solution and rebuild your DLLs.
2) Use the provided `ServiceProvider.cs` as a reference in your main app.
3) In the harness project, add Dependencies to your compiled `BroadcastBusClient.dll` and the assembly containing `IBroadcastBus`.
4) Adjust `HarnessApp/BroadcastBus.ConsoleHarness/appsettings.json` only.

### Run
```
nats-server -p 4222
dotnet run --project HarnessApp/BroadcastBus.ConsoleHarness
```
Open two or more windows. Type in one → appears in the others. `/me` prints current config, `/quit` exits.