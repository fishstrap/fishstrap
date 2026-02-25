Packaging instructions for Leafstrap

1) Build/publish the app (example):

```powershell
dotnet restore
dotnet build -c Release
dotnet publish Bloxstrap\Bloxstrap.csproj -c Release -r win-x64 --self-contained false /p:PublishSingleFile=true -o .\out\leafstrap
```

2) Create an installer (choose one):

- Inno Setup (recommended): install Inno Setup and run `ISCC leafstrap.iss` from `packaging/`.
- NSIS: install NSIS and run `makensis leafstrap.nsi` from `packaging/`.
- Quick ZIP: run the PowerShell helper below with `-Tool zip`.

3) The installer scripts expect the published exe at `out\leafstrap\Leafstrap.exe`.

Helper script:

```powershell
.\create-installer.ps1 -PublishDir .\out\leafstrap -Tool innosetup
```
