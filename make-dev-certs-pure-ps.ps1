<# 
make-dev-certs-pure-ps.ps1  (PowerShell-only; no OpenSSL)
Generates a local CA, a TLS server certificate (CN and SANs), and a client certificate for mTLS.

Outputs (in -OutDir):
  truststore.pem    (CA cert PEM)
  truststore.cer    (CA cert DER, for Windows trust import)
  server-cert.pem   (server certificate PEM)
  server-key.pem    (server PRIVATE KEY in PKCS#8 PEM)
  client.pfx        (client certificate PFX for .NET client usage)

Also imports the CA into LocalMachine\Root so the client trusts your server cert.

Usage (run as Administrator):
  .\make-dev-certs-pure-ps.ps1 -OutDir "C:\dev\pentonic\dev-certs" -DnsName "localhost" -Include127 $true -ClientPfxPassword "changeit"

You can re-run safely; it will create new certs each time.
#>

[CmdletBinding()]
param(
  [Parameter(Mandatory=$true)]
  [string]$OutDir,

  # Hostname you will use to connect (must be present in server cert SANs).
  [string]$DnsName = "localhost",

  # Whether to include 127.0.0.1 as a SAN alongside DnsName (useful when connecting via 127.0.0.1).
  [bool]$Include127 = $true,

  # Password for exported client PFX.
  [string]$ClientPfxPassword = "changeit"
)

Set-StrictMode -Version Latest

function Assert-Admin {
  $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
  $principal = New-Object Security.Principal.WindowsPrincipal($identity)
  if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    throw "Please run PowerShell as Administrator."
  }
}

# Write bytes as PEM with given header/footer (no OpenSSL required)
function Write-Pem {
  param(
    [Parameter(Mandatory=$true)][byte[]]$Bytes,
    [Parameter(Mandatory=$true)][string]$Path,
    [Parameter(Mandatory=$true)][string]$Header,
    [Parameter(Mandatory=$true)][string]$Footer
  )
  $b64 = [System.Convert]::ToBase64String($Bytes, [System.Base64FormattingOptions]::InsertLineBreaks)
  $content = @()
  $content += "-----BEGIN $Header-----"
  $content += $b64
  $content += "-----END $Footer-----"
  Set-Content -LiteralPath $Path -Value $content -NoNewline:$false -Encoding ascii
}

function Ensure-Dir([string]$p) { if (-not (Test-Path -LiteralPath $p)) { New-Item -ItemType Directory -Path $p | Out-Null } }

try {
  Assert-Admin
  Ensure-Dir $OutDir

  # 1) Create a local CA (exportable key)
  $ca = New-SelfSignedCertificate `
    -Type Custom `
    -KeyExportPolicy Exportable `
    -KeyUsage CertSign, CRLSign, DigitalSignature `
    -Subject "CN=Pentonic Dev CA" `
    -KeyAlgorithm RSA -KeyLength 2048 -HashAlgorithm SHA256 `
    -TextExtension @("2.5.29.19={critical}{text}CA=true") `
    -CertStoreLocation "Cert:\LocalMachine\My" `
    -NotAfter (Get-Date).AddYears(5)

  # Export CA as DER (.cer) and PEM (.pem)
  $trustCer = Join-Path $OutDir "truststore.cer"
  $null = Export-Certificate -Cert $ca -FilePath $trustCer
  $caDerBytes = [System.IO.File]::ReadAllBytes($trustCer)
  $trustPem = Join-Path $OutDir "truststore.pem"
  Write-Pem -Bytes $caDerBytes -Path $trustPem -Header "CERTIFICATE" -Footer "CERTIFICATE"

  # Import CA into Windows Root trust
  Import-Certificate -FilePath $trustCer -CertStoreLocation "Cert:\LocalMachine\Root" | Out-Null

  # 2) Create TLS server certificate (CN=$DnsName, SANs include $DnsName and optional 127.0.0.1)
  $sans = @($DnsName)
  if ($Include127) { $sans += "127.0.0.1" }

  $server = New-SelfSignedCertificate `
    -Type Custom `
    -KeyExportPolicy Exportable `
    -Subject ("CN={0}" -f $DnsName) `
    -DnsName $sans `
    -KeyAlgorithm RSA -KeyLength 2048 -HashAlgorithm SHA256 `
    -TextExtension @(
        "2.5.29.19={text}CA=false", 
        "2.5.29.37={text}1.3.6.1.5.5.7.3.1" # EKU: Server Authentication
     ) `
    -Signer $ca `
    -CertStoreLocation "Cert:\LocalMachine\My" `
    -NotAfter (Get-Date).AddYears(2)

  # Export server certificate PEM
  $serverCertPem = Join-Path $OutDir "server-cert.pem"
  $serverDer = $server.Export([System.Security.Cryptography.X509Certificates.X509ContentType]::Cert)
  Write-Pem -Bytes $serverDer -Path $serverCertPem -Header "CERTIFICATE" -Footer "CERTIFICATE"

  # Export server PRIVATE KEY as PKCS#8 PEM (no OpenSSL)
  $serverKeyPem = Join-Path $OutDir "server-key.pem"
  $rsa = $server.GetRSAPrivateKey()
  if (-not $rsa) { throw "Server certificate does not have an RSA private key." }
  $pkcs8 = $rsa.ExportPkcs8PrivateKey()
  Write-Pem -Bytes $pkcs8 -Path $serverKeyPem -Header "PRIVATE KEY" -Footer "PRIVATE KEY"

  # 3) Create client certificate (CN must match host if your code looks up by SubjectName)
  $client = New-SelfSignedCertificate `
    -Type Custom `
    -KeyExportPolicy Exportable `
    -Subject ("CN={0}" -f $DnsName) `
    -DnsName $DnsName `
    -KeyAlgorithm RSA -KeyLength 2048 -HashAlgorithm SHA256 `
    -TextExtension @(
        "2.5.29.19={text}CA=false",
        "2.5.29.37={text}1.3.6.1.5.5.7.3.2" # EKU: Client Authentication
     ) `
    -Signer $ca `
    -CertStoreLocation "Cert:\LocalMachine\My" `
    -NotAfter (Get-Date).AddYears(2)

  # Export client PFX (for app PFX mode) and also keep it in LocalMachine\My (for Windows Store mode)
  $clientPfx = Join-Path $OutDir "client.pfx"
  $secure = ConvertTo-SecureString -String $ClientPfxPassword -Force -AsPlainText
  Export-PfxCertificate -Cert $client -FilePath $clientPfx -Password $secure | Out-Null

  # Summary
  Write-Host ""
  Write-Host "✔ Certificates generated (PowerShell-only) in $OutDir"
  Write-Host "  - $trustPem"
  Write-Host "  - $trustCer  (CA imported into LocalMachine\\Root)"
  Write-Host "  - $serverCertPem"
  Write-Host "  - $serverKeyPem"
  Write-Host "  - $clientPfx"
  Write-Host ""
  Write-Host "Place files as you prefer and update your configs:"
  Write-Host "  NATS (nats.conf.tls):"
  Write-Host "    tls {"
  Write-Host "      cert_file:  \"<path-to>/server-cert.pem\""
  Write-Host "      key_file:   \"<path-to>/server-key.pem\""
  Write-Host "      ca_file:    \"<path-to>/truststore.pem\""
  Write-Host "      verify:     true"
  Write-Host "    }"
  Write-Host ""
  Write-Host "  App (appsettings.json, PFX mode):"
  Write-Host "    \"UseMtls\": true,"
  Write-Host "    \"Mtls\": {"
  Write-Host "      \"UseWindowsStoreClientCert\": false,"
  Write-Host "      \"ClientPfxPath\": \"<path-to>/client.pfx\","
  Write-Host "      \"ClientPfxPassword\": \"$ClientPfxPassword\","
  Write-Host "      \"TrustPemPath\": \"<path-to>/truststore.pem\""
  Write-Host "    }"
  Write-Host ""
  Write-Host "  App (Windows Store mode):"
  Write-Host "    \"UseWindowsStoreClientCert\": true  # cert in LocalMachine\\My; CN must equal host: $DnsName"
  Write-Host ""
  Write-Host "Done."
}
catch {
  Write-Error $_
  exit 1
}
