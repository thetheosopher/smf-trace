#ifndef AppVersion
  #error AppVersion define is required.
#endif

#ifndef FileVersion
  #define FileVersion AppVersion
#endif

#ifndef PublishDir
  #error PublishDir define is required.
#endif

#ifndef OutputDir
  #error OutputDir define is required.
#endif

#ifndef OutputBaseFilename
  #define OutputBaseFilename "SMFTrace-setup-win-x64"
#endif

[Setup]
AppId={{5A4C4F69-4DF2-4C25-8C6B-0E9E2E5B8E9A}
AppName=SMFTrace
AppVersion={#AppVersion}
AppVerName=SMFTrace {#AppVersion}
AppPublisher=SMF Trace Contributors
AppPublisherURL=https://github.com/thetheosopher/smf-trace
AppSupportURL=https://github.com/thetheosopher/smf-trace/issues
AppUpdatesURL=https://github.com/thetheosopher/smf-trace/releases
DefaultDirName={autopf}\SMFTrace
DefaultGroupName=SMF Trace
AllowNoIcons=yes
PrivilegesRequired=admin
PrivilegesRequiredOverridesAllowed=dialog
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
DisableProgramGroupPage=yes
OutputDir={#OutputDir}
OutputBaseFilename={#OutputBaseFilename}
SetupIconFile=..\SMFTrace.Wpf\Resources\app-icon.ico
UninstallDisplayIcon={app}\SMFTrace.exe
VersionInfoVersion={#FileVersion}
VersionInfoProductName=SMFTrace
VersionInfoProductVersion={#AppVersion}
VersionInfoDescription=SMFTrace Setup

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\SMF Trace"; Filename: "{app}\SMFTrace.exe"; WorkingDir: "{app}"
Name: "{autodesktop}\SMF Trace"; Filename: "{app}\SMFTrace.exe"; WorkingDir: "{app}"; Tasks: desktopicon
Name: "{autoprograms}\Uninstall SMF Trace"; Filename: "{uninstallexe}"

[Run]
Filename: "{app}\SMFTrace.exe"; Description: "{cm:LaunchProgram,SMFTrace}"; Flags: nowait postinstall skipifsilent