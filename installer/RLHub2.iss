; Inno Setup script for RL Hub 2.
;
; Build it with build-installer.ps1, which publishes the app first and passes the version
; from the csproj — so the version lives in one place instead of drifting between the two.
;
; Installs per-user (no UAC prompt): a personal desktop app has no business asking for
; administrator rights. User data in %LocalAppData%\RLHub2 is deliberately left alone on
; uninstall — matches, MMR history and settings are the user's, not the installer's.

#ifndef AppVersion
  #define AppVersion "1.0.0"
#endif

#define AppName "RL Hub 2"
#define AppExe "RLHub2.exe"
#define AppPublisher "micha"

[Setup]
AppId={{8F3C2A61-4D7E-4C9B-9E2A-51B7D4A6C8F2}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher={#AppPublisher}
DefaultDirName={autopf}\RLHub2
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
DisableDirPage=no
; per-user install -> no administrator prompt
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
OutputDir=Output
OutputBaseFilename=RLHub2-Setup-{#AppVersion}
SetupIconFile=..\RLHub2\Resources\app.ico
UninstallDisplayIcon={app}\{#AppExe}
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
; the app is x64-only (self-contained win-x64 publish)
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

[Languages]
Name: "polish"; MessagesFile: "compiler:Languages\Polish.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; the whole publish folder: the app is self-contained, so the .NET runtime ships with it
Source: "..\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExe}"
Name: "{group}\{cm:UninstallProgram,{#AppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExe}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExe}"; Description: "{cm:LaunchProgram,{#AppName}}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; only what the installer itself created; %LocalAppData%\RLHub2 (matches, settings) stays
Type: filesandordirs; Name: "{app}"
