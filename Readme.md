Got it — let’s do this **PowerShell-only** (no OpenSSL anywhere).

Below is a single script you can run as-is to generate all certs using just Windows PowerShell + .NET APIs. It outputs **CA, server (PEM + PKCS#8 key), and client (PFX)** into a folder you choose, and it **imports the CA** into `LocalMachine\Root` so your client can validate the server.

**Download:**
[make-dev-certs-pure-ps.ps1](sandbox:/mnt/data/dev-certs/make-dev-certs-pure-ps.ps1)

---

# 1) Generate certs (PowerShell only)

Open **PowerShell as Administrator**, cd to where you saved the script, then run:

```powershell
.\make-dev-certs-pure-ps.ps1 -OutDir "C:\dev\pentonic\dev-certs" -DnsName "localhost" -Include127 $true -ClientPfxPassword "changeit"
```

This creates in `C:\dev\pentonic\dev-certs`:

* `truststore.pem` (CA for NATS server `ca_file`)
* `truststore.cer` (same CA in DER; already imported to **LocalMachine\Root**)
* `server-cert.pem` (server cert)
* `server-key.pem` (**PKCS#8** private key; NATS friendly)
* `client.pfx` (client cert with private key; use in your console app **PFX mode**)

> If your app connects to a **different host** (e.g., `nats.dev.local`), re-run with `-DnsName "nats.dev.local"`. The server cert SANs will include that name; your app’s `HostEndPointUrl` must use the same host.

---

# 2) Place files in your folders (you choose the layout)

You said you’ll align with your new structure manually — perfect. Here’s a clean mapping you can adapt:

**NATS server machine / folder (where `nats-server.exe` runs):**

```
<your-nats-root>\
  nats.conf.tls
  certs\
    server-cert.pem
    server-key.pem
    truststore.pem
```

**Your console app project folder:**

```
<your-app-root>\
  appsettings.json
  certs\
    client.pfx
```

If you prefer a central “dev-certs” stash (e.g., `C:\dev\pentonic\dev-certs`), you can keep them there and just reference absolute paths in config.

---

# 3) Update configs to your paths

## NATS `nats.conf.tls`

```hocon
port: 4222
authorization {
  users = [
    { user: "pentonic", password: "pentonic" }
  ]
}
tls {
  cert_file:  "C:\\dev\\pentonic\\dev-certs\\server-cert.pem"   # <-- your path
  key_file:   "C:\\dev\\pentonic\\dev-certs\\server-key.pem"    # <-- your path
  ca_file:    "C:\\dev\\pentonic\\dev-certs\\truststore.pem"    # <-- your path
  verify:     true                                              # require client cert (mTLS)
}
```

## Your app `appsettings.json`

### Option A — **PFX mode** (uses the `client.pfx` file)

```json
"NatsServer": {
  "HostEndPointUrl": "localhost:4222",
  "Username": "pentonic",
  "Password": "pentonic",
  "UseMtls": true,
  "Mtls": {
    "UseWindowsStoreClientCert": false,
    "ClientPfxPath": "C:\\dev\\pentonic\\dev-certs\\client.pfx",
    "ClientPfxPassword": "changeit",
    "TrustPemPath": "C:\\dev\\pentonic\\dev-certs\\truststore.pem"  // optional if CA imported to Root
  }
}
```

### Option B — **Windows Store mode** (no PFX path; subject lookup)

```json
"NatsServer": {
  "HostEndPointUrl": "localhost:4222",
  "Username": "pentonic",
  "Password": "pentonic",
  "UseMtls": true,
  "Mtls": {
    "UseWindowsStoreClientCert": true
    // Your code finds a cert in LocalMachine\My whose SubjectName == "localhost"
    // (since you ran the script with -DnsName localhost). Ensure it has a private key.
  }
}
```

> Pick **either** A or B. Your code already supports both patterns.

---

# 4) Run checklist

1. **Start NATS with TLS/mTLS config**

```powershell
.\nats-server.exe -c C:\path\to\nats.conf.tls -DV
```

2. **Run your console app**

```powershell
dotnet run --project <your-app-root>\<YourApp>.csproj
```

3. **Test a flow**

```
add A 23,Alice
show A
evict A 23
show A
clear B
show B
```

You should see EVICT and CLEAR events fanning out across AP1..AP3 and BP1..BP3 as per your subjects.

---

## Notes & common snags (PowerShell-only path)

* The script already **imports the CA** into `LocalMachine\Root`. If you prefer not to rely on Windows trust, keep `Mtls.TrustPemPath` pointing at your `truststore.pem`.
* **Hostname/SAN** must match: the `HostEndPointUrl` host (e.g., `localhost`) must appear in the server cert SANs (the script handles this via `-DnsName`).
* **Windows Store mode**: cert must be in **LocalMachine\My**, have a **private key**, and **CN = host**.
* **PKCS#8 key format**: the script exports `server-key.pem` as **PKCS#8** via .NET (`ExportPkcs8PrivateKey()`), which NATS expects.

If you want, tell me your exact new folder paths (NATS cert dir and app cert dir) and I’ll rewrite the two config snippets above with those literal paths filled in.
