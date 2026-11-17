#ifndef AppVersion
#define AppVersion "1.0.0"
#endif
#ifndef SourceDir
#define SourceDir "..\publish"
#endif

[Setup]
AppId={{6B1F2C0E-5D0B-4B8E-9C55-7A3F0E5C1A42}
AppName=ScrollShot
AppVersion={#AppVersion}
AppPublisher=ScrollShot
DefaultDirName={autopf}\ScrollShot
DefaultGroupName=ScrollShot
DisableProgramGroupPage=yes
OutputDir=..\installer-output
OutputBaseFilename=ScrollShot-Setup-{#AppVersion}
SetupIconFile=..\app.ico
UninstallDisplayIcon={app}\ScrollShot.exe
Compression=lzma2
SolidCompression=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
WizardStyle=modern
CloseApplications=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "persian"; MessagesFile: "compiler:Languages\Farsi.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; Flags: unchecked
Name: "startup"; Description: "Start ScrollShot with Windows"; Flags: unchecked

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\ScrollShot"; Filename: "{app}\ScrollShot.exe"
Name: "{autodesktop}\ScrollShot"; Filename: "{app}\ScrollShot.exe"; Tasks: desktopicon

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "ScrollShot"; ValueData: """{app}\ScrollShot.exe"""; Tasks: startup; Flags: uninsdeletevalue

[Run]
Filename: "{app}\ScrollShot.exe"; Description: "{cm:LaunchProgram,ScrollShot}"; Flags: nowait postinstall skipifsilent
