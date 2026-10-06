#ifndef MyAppVersion
  #define MyAppVersion "0.1.0"
#endif
#ifndef PublishDir
  #define PublishDir "..\artifacts\publish"
#endif

[Setup]
AppId={{A3F87C16-D4F4-4B5D-A05D-49AE50245921}
AppName=LouderMe
AppVersion={#MyAppVersion}
AppPublisher=Michel's Lab
DefaultDirName={autopf}\LouderMe
DefaultGroupName=LouderMe
DisableProgramGroupPage=yes
OutputDir=..\artifacts
OutputBaseFilename=LouderMe-Setup-v{#MyAppVersion}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequiredOverridesAllowed=dialog
SetupIconFile=..\src\LouderMeDesktop\Assets\LouderMe.ico
UninstallDisplayIcon={app}\LouderMe.exe

[Files]
Source: "{#PublishDir}\LouderMe.exe"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\LouderMe"; Filename: "{app}\LouderMe.exe"; IconFilename: "{app}\LouderMe.exe"
Name: "{autodesktop}\LouderMe"; Filename: "{app}\LouderMe.exe"; IconFilename: "{app}\LouderMe.exe"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional options:"; Flags: unchecked
Name: "startup"; Description: "Start LouderMe with Windows"; GroupDescription: "Additional options:"; Flags: unchecked

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "LouderMe"; ValueData: """{app}\LouderMe.exe"" --startup"; Tasks: startup; Flags: uninsdeletevalue

[Run]
Filename: "{app}\LouderMe.exe"; Description: "Launch LouderMe"; Flags: nowait postinstall skipifsilent
