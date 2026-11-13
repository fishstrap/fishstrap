# XPLSTRAP Windows installer

XPLSTRAP publishes two Windows x64 executables from the same self-contained application build:

- **XPLSTRAP-Setup.exe** — the recommended first-download entry point. On a fresh machine/profile, launching it opens XPLSTRAP's built-in first-run installation wizard.
- **XPLSTRAP.exe** — the regular launcher executable. After installation, this is the executable installed into the XPLSTRAP application directory and used by shortcuts and Roblox protocol registration.

## Why both files are published

The setup executable is a separately named copy of the launcher, not a second installer implementation. This intentionally reuses the existing installation flow in the application, including its language selection, install-location validation, shortcut creation, settings import, registry registration, and uninstall support. Keeping one installation implementation avoids having two installers overwrite each other's files or registry entries.

Both files are built from the same commit and use the same XPLSTRAP icon, version, and self-contained .NET publish output. The workflow verifies that the launcher exists before creating the setup copy and uploads both as build artifacts. Version-tagged and test-tagged GitHub releases attach both executables.

## Build locally

From a Windows machine with the .NET 6 SDK installed:

```powershell
dotnet restore
dotnet publish -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -r win-x64 -c Release --self-contained true .\Bloxstrap\Bloxstrap.csproj

$publishDir = ".\Bloxstrap\bin\Release\net6.0-windows\win-x64\publish"
Copy-Item "$publishDir\XPLSTRAP.exe" "$publishDir\XPLSTRAP-Setup.exe" -Force
```

The generated files are in the publish directory. The GitHub Actions workflow performs these steps automatically.
