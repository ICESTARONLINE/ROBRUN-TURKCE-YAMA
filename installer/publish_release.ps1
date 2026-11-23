# Release: publish + SHA256 + (opsiyonel) Authenticode imza + site dosyalari
# Kullanim:
#   .\publish_release.ps1
#   .\publish_release.ps1 -PfxPath "C:\certs\codesign.pfx"   # CODESIGN_PFX_PASSWORD env ile

param(
    [string] $PfxPath = $env:CODESIGN_PFX_PATH,
    [string] $Configuration = "Release",
    [string] $OutDir = "D:\robrun\publish_out",
    [string] $SiteDir = "D:\robrun\website"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $root

Write-Host "==1/5 Integrity.json yenile==" -ForegroundColor Cyan
$patch = Join-Path $root "PatchData"
$files = @(
    "tr_cache_worker.exe",
    "inbuild_assets_all_turkish_.bundle",
    "localization-string-tables-turkish(turkey-republic)(tr)_assets_all_turkish_.bundle"
)
$map = [ordered]@{}
foreach ($f in $files) {
    $p = Join-Path $patch $f
    if (Test-Path $p) {
        $map[$f] = (Get-FileHash -Algorithm SHA256 -LiteralPath $p).Hash
    }
}
$integrity = [ordered]@{
    version      = "1.0"
    product      = "ROB RUN Turkce Yama"
    publisher    = "ICESTAR"
    homepage     = "https://robrunturkceyama.com/"
    files        = $map
    writeTargets = @("%USERPROFILE%\AppData\LocalLow\Kefir\ROB RUN\com.unity.addressables\")
    notes        = "Steam oyun klasoru kurulumda degistirilmez; sadece Addressables onbellege yazilir."
}
$integrityPath = Join-Path $patch "integrity.json"
$integrity | ConvertTo-Json -Depth 5 | Set-Content -Encoding UTF8 -LiteralPath $integrityPath

Write-Host "==2/5 Publish==" -ForegroundColor Cyan
dotnet publish ".\RobRunTurkceYama.csproj" -c $Configuration -r win-x64 -p:Platform=x64 --self-contained true -o $OutDir
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed" }

$exe = Join-Path $OutDir "ROBRUN_TR.exe"
if (-not (Test-Path $exe)) { throw "EXE yok: $exe" }

Write-Host "==3/5 Authenticode (opsiyonel)==" -ForegroundColor Cyan
if ($PfxPath -and (Test-Path $PfxPath)) {
    & "$root\sign_exe.ps1" -ExePath $exe -PfxPath $PfxPath
} else {
    Write-Host "PFX yok — imzasiz build. Sertifika sonrasi: .\sign_exe.ps1 -ExePath `"$exe`" -PfxPath <pfx>" -ForegroundColor Yellow
}

Write-Host "==4/5 SHA256 + release.json==" -ForegroundColor Cyan
$hash = (Get-FileHash -Algorithm SHA256 -LiteralPath $exe).Hash
$releaseDir = Join-Path $root "release"
New-Item -ItemType Directory -Force -Path $releaseDir | Out-Null
$sums = Join-Path $releaseDir "SHA256SUMS.txt"
"$hash  ROBRUN_TR.exe" | Set-Content -Encoding ascii -LiteralPath $sums

$releaseJson = [ordered]@{
    version     = "1.0"
    sha256      = $hash
    downloadUrl = "https://github.com/ICESTARONLINE/ROBRUN-TURKCE-YAMA/releases/latest/download/ROBRUN_TR.exe"
    notes       = "ROB RUN Turkce Yama 1.0"
    product     = "ROB RUN Turkce Yama"
    publisher   = "ICESTAR"
}
$releaseJson | ConvertTo-Json | Set-Content -Encoding UTF8 -LiteralPath (Join-Path $releaseDir "release.json")

$fp = @"
Windows Defender / SmartScreen — yanlis pozitif bildirimi
========================================================
1) https://www.microsoft.com/wdsi/filesubmission
2) Software developer → Submit a file
3) Dosya: ROBRUN_TR.exe
4) SHA-256: $hash
5) Aciklama (EN/TR):
   Fan-made Turkish localization installer for the Steam game ROB RUN.
   Writes only to %USERPROFILE%\AppData\LocalLow\Kefir\ROB RUN\com.unity.addressables\
   Does not modify Steam game binaries. No admin required. Publisher: ICESTAR.
   Homepage: https://robrunturkceyama.com/

Authenticode: OV/EV sertifikasi ile imzala (sign_exe.ps1).
"@
Set-Content -Encoding UTF8 -LiteralPath (Join-Path $releaseDir "DEFENDER_SUBMISSION.txt") -Value $fp

Write-Host "==5/5 Siteye kopyala==" -ForegroundColor Cyan
Copy-Item -Force $exe (Join-Path $SiteDir "ROBRUN_TR.exe")
Copy-Item -Force $exe "D:\robrun\ROBRUN_TR.exe"
Copy-Item -Force (Join-Path $releaseDir "release.json") (Join-Path $SiteDir "release.json")
Copy-Item -Force $sums (Join-Path $SiteDir "SHA256SUMS.txt")

Write-Host ""
Write-Host "OK  $exe" -ForegroundColor Green
Write-Host "SHA256  $hash"
Write-Host "Site: $SiteDir\ROBRUN_TR.exe + release.json + SHA256SUMS.txt"
Write-Host "Defender: $releaseDir\DEFENDER_SUBMISSION.txt"
