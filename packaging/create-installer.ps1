param(
    [string]$PublishDir = ".\out\leafstrap",
    [ValidateSet('innosetup','nsis','zip')][string]$Tool = 'innosetup',
    [string]$OutputDir = ".\out\installer"
)

if (-not (Test-Path $PublishDir)) {
    Write-Error "Publish directory not found: $PublishDir"
    exit 1
}

if (-not (Test-Path $OutputDir)) { New-Item -ItemType Directory -Path $OutputDir | Out-Null }

$exe = Join-Path $PublishDir 'Leafstrap.exe'
if (-not (Test-Path $exe)) {
    Write-Error "Expected executable not found: $exe"
    exit 1
}

switch ($Tool) {
    'innosetup' {
        $iss = Join-Path $PSScriptRoot 'leafstrap.iss'
        if (-not (Test-Path $iss)) { Write-Error "Inno Setup script missing: $iss"; exit 1 }
        if (-not (Get-Command iscc -ErrorAction SilentlyContinue)) { Write-Error "ISCC (Inno Setup Compiler) not found in PATH"; exit 1 }
        & iscc /O"$OutputDir" /F"leafstrap-3.0.2-setup" $iss
        exit $LASTEXITCODE
    }
    'nsis' {
        $nsi = Join-Path $PSScriptRoot 'leafstrap.nsi'
        if (-not (Test-Path $nsi)) { Write-Error "NSIS script missing: $nsi"; exit 1 }
        if (-not (Get-Command makensis -ErrorAction SilentlyContinue)) { Write-Error "makensis not found in PATH"; exit 1 }
        & makensis /DOUTDIR="$OutputDir" $nsi
        exit $LASTEXITCODE
    }
    'zip' {
        $zip = Join-Path $OutputDir 'leafstrap-3.0.2.zip'
        if (Test-Path $zip) { Remove-Item $zip }
        Add-Type -AssemblyName System.IO.Compression.FileSystem
        [IO.Compression.ZipFile]::CreateFromDirectory($PublishDir, $zip)
        Write-Output "Created $zip"
    }
}
