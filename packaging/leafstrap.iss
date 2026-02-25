[Setup]
AppName=Leafstrap
AppVersion=3.0.2
DefaultDirName={localappdata}\Leafstrap
DefaultGroupName=Leafstrap
DisableProgramGroupPage=yes
OutputBaseFilename=leafstrap-3.0.2-setup
Compression=lzma
SolidCompression=yes

[Files]
; Source executable must be published to out\leafstrap\Leafstrap.exe
Source: "{#SourceExe}"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\Leafstrap"; Filename: "{app}\Leafstrap.exe"
Name: "{commondesktop}\Leafstrap"; Filename: "{app}\Leafstrap.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\Leafstrap.exe"; Description: "Launch Leafstrap"; Flags: nowait postinstall skipifsilent

; Note: replace {#SourceExe} with the actual path before calling ISCC,
; or call ISCC with /DSourceExe="path\\to\\out\\leafstrap\\Leafstrap.exe"
