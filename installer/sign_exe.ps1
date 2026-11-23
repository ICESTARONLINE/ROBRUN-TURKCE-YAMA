# ROBRUN_TR.exe Authenticode imzalama
# Gereksinim: OV/EV Code Signing sertifikasi (.pfx) veya Windows Certificate Store
#
# Kullanim:
#   .\sign_exe.ps1 -ExePath "D:\robrun\publish_out\ROBRUN_TR.exe" -PfxPath "C:\certs\codesign.pfx"
#   $env:CODESIGN_PFX_PASSWORD = "..." ; .\sign_exe.ps1 -ExePath "...\ROBRUN_TR.exe" -PfxPath "...\codesign.pfx"
#
# Store'da sertifika varsa (PFX yok):
#   .\sign_exe.ps1 -ExePath "...\ROBRUN_TR.exe" -UseStore

param(
    [Parameter(Mandatory = $true)]
    [string] $ExePath,

    [string] $PfxPath = $env:CODESIGN_PFX_PATH,
    [string] $PfxPassword = $env:CODESIGN_PFX_PASSWORD,
    [switch] $UseStore,
    [string] $TimestampUrl = "http://timestamp.digicert.com"
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $ExePath)) {
    throw "EXE yok: $ExePath"
}

$signtool = @(
    "${env:ProgramFiles(x86)}\Windows Kits\10\bin\10.0.26100.0\x64\signtool.exe",
    "${env:ProgramFiles(x86)}\Windows Kits\10\bin\10.0.22621.0\x64\signtool.exe"
) | Where-Object { Test-Path $_ } | Select-Object -First 1

if (-not $signtool) {
    $found = Get-ChildItem "${env:ProgramFiles(x86)}\Windows Kits\10\bin" -Recurse -Filter signtool.exe -ErrorAction SilentlyContinue |
        Where-Object { $_.FullName -match '\\x64\\signtool\.exe$' } |
        Select-Object -First 1
    $signtool = $found.FullName
}

if (-not $signtool) {
    throw "signtool.exe bulunamadi. Windows SDK kur."
}

$args = @(
    "sign",
    "/fd", "SHA256",
    "/td", "SHA256",
    "/tr", $TimestampUrl
)

if ($UseStore -or [string]::IsNullOrWhiteSpace($PfxPath)) {
    if (-not $UseStore -and [string]::IsNullOrWhiteSpace($PfxPath)) {
        $storeCerts = Get-ChildItem Cert:\CurrentUser\My -CodeSigningCert -ErrorAction SilentlyContinue
        if (-not $storeCerts) {
            $storeCerts = Get-ChildItem Cert:\LocalMachine\My -CodeSigningCert -ErrorAction SilentlyContinue
        }
        if (-not $storeCerts) {
            Write-Host @"

[!] Bu PC'de code signing sertifikasi YOK.

SmartScreen icin:
  1) DigiCert / Sectigo / SSL.com'dan OV veya EV Code Signing al
  2) .pfx'i guvenli yere koy
  3) Su komutu calistir:

     `$env:CODESIGN_PFX_PASSWORD = 'sifren'
     .\sign_exe.ps1 -ExePath '$ExePath' -PfxPath 'C:\path\codesign.pfx'

Self-signed sertifika SmartScreen'i DUZELTMEZ — alma.

"@
            exit 2
        }
    }
    $args += @("/a")
}
else {
    if (-not (Test-Path -LiteralPath $PfxPath)) {
        throw "PFX yok: $PfxPath"
    }
    if ([string]::IsNullOrEmpty($PfxPassword)) {
        $secure = Read-Host "PFX sifresi" -AsSecureString
        $PfxPassword = [Runtime.InteropServices.Marshal]::PtrToStringAuto(
            [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure))
    }
    $args += @("/f", $PfxPath, "/p", $PfxPassword)
}

$args += $ExePath

Write-Host "Imzalaniyor: $ExePath"
& $signtool @args
if ($LASTEXITCODE -ne 0) {
    throw "signtool sign basarisiz (exit $LASTEXITCODE)"
}

& $signtool verify /pa /v $ExePath
if ($LASTEXITCODE -ne 0) {
    throw "signtool verify basarisiz"
}

Write-Host "OK — imza + timestamp tamam."
