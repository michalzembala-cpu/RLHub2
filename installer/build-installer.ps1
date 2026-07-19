# Builds a release of RL Hub 2 and packages it into an installer.
#
#   .\installer\build-installer.ps1
#
# Publishes the app (self-contained, so users need no .NET install), then compiles the Inno
# Setup script. The version comes from the csproj, so it is defined in exactly one place.

$ErrorActionPreference = "Stop"

$root      = Split-Path $PSScriptRoot -Parent
$csproj    = Join-Path $root "RLHub2\RLHub2.csproj"
$publish   = Join-Path $root "publish"
$iss       = Join-Path $PSScriptRoot "RLHub2.iss"

# ---- version: single source of truth is the csproj ----
$version = ([xml](Get-Content $csproj)).Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1
if (-not $version) { throw "Nie znaleziono <Version> w $csproj" }
Write-Host "Wersja: $version" -ForegroundColor Cyan

# ---- find the Inno Setup compiler ----
# Ask the registry first: Inno Setup can live on any drive (it was installed to D:\ here),
# so a list of hardcoded C:\ paths finds nothing and claims it isn't installed.
$iscc = $null
$uninstallKeys = @(
    "HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\*",
    "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\*",
    "HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\*"
)
$installed = Get-ItemProperty $uninstallKeys -ErrorAction SilentlyContinue |
             Where-Object { $_.DisplayName -like "*Inno Setup*" -and $_.InstallLocation }
foreach ($i in $installed) {
    $candidate = Join-Path $i.InstallLocation "ISCC.exe"
    if (Test-Path $candidate) { $iscc = $candidate; break }
}

if (-not $iscc) {
    $iscc = @(
        "C:\Program Files (x86)\Inno Setup 6\ISCC.exe", "C:\Program Files\Inno Setup 6\ISCC.exe",
        "D:\Program Files (x86)\Inno Setup 6\ISCC.exe", "D:\Program Files\Inno Setup 6\ISCC.exe",
        "C:\Program Files (x86)\Inno Setup 7\ISCC.exe", "D:\Program Files (x86)\Inno Setup 7\ISCC.exe"
    ) | Where-Object { Test-Path $_ } | Select-Object -First 1
}

if (-not $iscc) {
    Write-Host "`nNie znaleziono Inno Setup (ISCC.exe)." -ForegroundColor Yellow
    Write-Host "Zainstaluj go: winget install JRSoftware.InnoSetup"
    Write-Host "albo pobierz z https://jrsoftware.org/isdl.php`n"
    throw "Brak Inno Setup"
}

# ---- publish ----
# A running instance locks the exe and the copy fails half-way through.
Get-Process RLHub2 -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 400

Write-Host "Publikuję..." -ForegroundColor Cyan
if (Test-Path $publish) { Remove-Item $publish -Recurse -Force }
dotnet publish $csproj -c Release -o $publish | Out-Null
if ($LASTEXITCODE -ne 0) { throw "dotnet publish nie powiodlo sie" }

# The app loads these from Resources\ next to the exe; without them it starts but has no
# backdrops. Cheap to check here, and it caught a real packaging bug once.
foreach ($f in @("cs2_bg.png", "gamepicker_bg.png", "overlay_bg.png", "game_rl.jpg", "game_cs2.jpg")) {
    if (-not (Test-Path (Join-Path $publish "Resources\$f"))) { throw "Brak zasobu w publish: $f" }
}
$size = [math]::Round((Get-ChildItem $publish -Recurse -File | Measure-Object Length -Sum).Sum / 1MB, 1)
Write-Host "  publish: $size MB" -ForegroundColor DarkGray

# ---- compile the installer ----
Write-Host "Buduję instalator..." -ForegroundColor Cyan
& $iscc "/DAppVersion=$version" $iss | Out-Null
if ($LASTEXITCODE -ne 0) { throw "ISCC nie powiodlo sie" }

$setup = Join-Path $PSScriptRoot "Output\RLHub2-Setup-$version.exe"
$mb = [math]::Round((Get-Item $setup).Length / 1MB, 1)
Write-Host "`nGotowe: $setup ($mb MB)" -ForegroundColor Green
Write-Host "Wrzuc ten plik jako zasob wydania na GitHubie z tagiem v$version." -ForegroundColor DarkGray
