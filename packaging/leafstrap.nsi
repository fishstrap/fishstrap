; NSIS installer for Leafstrap
!define APPNAME "Leafstrap"
!define VERSION "3.0.2"
!define OUTFILE "leafstrap-3.0.2-setup.exe"
!define INSTALLDIR "$LOCALAPPDATA\\Leafstrap"

SetCompressor /SOLID lzma
Name "${APPNAME} ${VERSION}"
OutFile "${OUTFILE}"
InstallDir ${INSTALLDIR}
RequestExecutionLevel user

Section "Install"
  SetOutPath "$INSTDIR"
  ; The published exe must be provided in the same folder as this script or full path substituted
  File "..\\out\\leafstrap\\Leafstrap.exe"
  CreateShortCut "$SMPROGRAMS\\${APPNAME}\\${APPNAME}.lnk" "$INSTDIR\\Leafstrap.exe"
  CreateShortCut "$DESKTOP\\${APPNAME}.lnk" "$INSTDIR\\Leafstrap.exe"
SectionEnd

Section "Uninstall"
  Delete "$INSTDIR\\Leafstrap.exe"
  Delete "$SMPROGRAMS\\${APPNAME}\\${APPNAME}.lnk"
  Delete "$DESKTOP\\${APPNAME}.lnk"
  RMDir "$INSTDIR"
  RMDir "$SMPROGRAMS\\${APPNAME}"
SectionEnd
