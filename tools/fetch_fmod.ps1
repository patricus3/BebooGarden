<#
Puts FMOD's Android native libraries where the build expects them.

FMOD is not redistributable, so the archive itself cannot live in this repository and cannot be
downloaded without an account - fmod.com puts every download behind a login. So this does not
pretend to fetch it unattended. What it does:

  - says nothing and exits if the libraries are already in place;
  - finds an FMOD Android archive you have already downloaded, anywhere obvious, and unpacks the
    right .so for each ABI out of it;
  - otherwise tells you exactly what to get and opens the download page.

Give it a path if the archive is somewhere unusual:
    tools\fetch_fmod.ps1 -Archive D:\stuff\fmodstudioapi20228android.tar.gz
#>

[CmdletBinding()]
param(
    [string]$Archive,
    [switch]$Quiet
)

$ErrorActionPreference = 'Stop'

$repo = Split-Path -Parent $PSScriptRoot
$libRoot = Join-Path $repo 'BebooGarden.Android\lib\android'
$abis = @('arm64-v8a', 'armeabi-v7a', 'x86_64')

function Write-Step($text) { if (-not $Quiet) { Write-Host $text } }

# --- Already done? -----------------------------------------------------------
$present = $abis | Where-Object { Test-Path (Join-Path $libRoot "$_\libfmod.so") }
if ($present.Count -gt 0) {
    Write-Step "  FMOD already in place for: $($present -join ', ')"
    exit 0
}

# --- Find an archive ---------------------------------------------------------
if (-not $Archive) {
    $searchIn = @(
        "$env:USERPROFILE\Downloads"
        "$env:USERPROFILE\Desktop"
        $repo
    ) | Where-Object { Test-Path $_ }

    $Archive = Get-ChildItem $searchIn -File -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -match 'fmod.*android.*\.(zip|tar\.gz|tgz)$' } |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1 -ExpandProperty FullName
}

if (-not $Archive -or -not (Test-Path $Archive)) {
    Write-Host ""
    Write-Host "  FMOD's Android libraries are missing, and they cannot be downloaded for you:"
    Write-Host "  every fmod.com download needs an account, and the licence does not allow"
    Write-Host "  shipping them in this repository."
    Write-Host ""
    Write-Host "    1. Sign in (free) at https://www.fmod.com/download"
    Write-Host "    2. Take FMOD Engine, platform Android"
    Write-Host "    3. Leave the archive in your Downloads folder"
    Write-Host "    4. Run this again - it will find it and unpack it"
    Write-Host ""
    Write-Host "  Or point it straight at the file:"
    Write-Host "    tools\fetch_fmod.ps1 -Archive <path to the archive>"
    Write-Host ""
    Write-Host "  Without it the game still builds, installs, talks and unpacks its sounds."
    Write-Host "  It fails only when it asks FMOD for an audio system."
    Write-Host ""
    if (-not $Quiet) { Start-Process 'https://www.fmod.com/download#fmodengine' }
    exit 2
}

Write-Step "  Using $Archive"

# --- Unpack it ---------------------------------------------------------------
$staging = Join-Path ([System.IO.Path]::GetTempPath()) "fmod-android-$(Get-Random)"
New-Item -ItemType Directory -Path $staging -Force | Out-Null

try {
    if ($Archive -match '\.zip$') {
        Expand-Archive -Path $Archive -DestinationPath $staging -Force
    }
    else {
        # tar ships with Windows 10 1803 and later, and handles .tar.gz directly.
        & tar -xf $Archive -C $staging
        if ($LASTEXITCODE -ne 0) { throw "tar could not read $Archive" }
    }

    # libfmodL.so is the logging build - bigger, slower, and not what ships.
    $found = Get-ChildItem $staging -Recurse -File -Filter 'libfmod.so' -ErrorAction SilentlyContinue

    if (-not $found) {
        Write-Host "  No libfmod.so inside that archive. Is it the Android build of FMOD Engine?"
        exit 3
    }

    $copied = 0
    foreach ($abi in $abis) {
        # The ABI is the name of the folder the library sits in, inside api/core/lib/<abi>/.
        $source = $found | Where-Object { (Split-Path $_.DirectoryName -Leaf) -eq $abi } | Select-Object -First 1
        if (-not $source) { continue }

        $target = Join-Path $libRoot $abi
        New-Item -ItemType Directory -Path $target -Force | Out-Null
        Copy-Item $source.FullName (Join-Path $target 'libfmod.so') -Force
        Write-Step ("  {0,-14} {1:N1} MB" -f $abi, ($source.Length / 1MB))
        $copied++
    }

    if ($copied -eq 0) {
        Write-Host "  Found libfmod.so but not for any ABI this build uses ($($abis -join ', '))."
        exit 3
    }

    Write-Step "  FMOD ready for $copied ABI(s)."
    exit 0
}
finally {
    Remove-Item $staging -Recurse -Force -ErrorAction SilentlyContinue
}
