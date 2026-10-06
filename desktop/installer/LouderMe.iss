#ifndef MyAppVersion
  #define MyAppVersion "0.1.0"
#endif
#ifndef PublishDir
  #define PublishDir "..\artifacts\publish"
#endif

[Setup]
AppId={{6B84E1C1-E48A-4B2E-9C6B-29EB7B4E7D7A}
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
CloseApplications=yes
CloseApplicationsFilter=LouderMe.exe
SetupIconFile=..\src\LouderMeDesktop\Assets\LouderMe.ico
UninstallDisplayIcon={app}\LouderMe.exe

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"

[CustomMessages]
english.DesktopShortcut=Create a desktop shortcut
spanish.DesktopShortcut=Crear un acceso directo en el escritorio
english.AdditionalIcons=Additional icons:
spanish.AdditionalIcons=Iconos adicionales:
english.LaunchApp=Launch LouderMe
spanish.LaunchApp=Abrir LouderMe

[Files]
Source: "{#PublishDir}\LouderMe.exe"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\LouderMe"; Filename: "{app}\LouderMe.exe"; IconFilename: "{app}\LouderMe.exe"
Name: "{autodesktop}\LouderMe"; Filename: "{app}\LouderMe.exe"; IconFilename: "{app}\LouderMe.exe"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "{cm:DesktopShortcut}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Run]
Filename: "{app}\LouderMe.exe"; Description: "{cm:LaunchApp}"; Flags: nowait postinstall skipifsilent


[UninstallRun]
Filename: "{cmd}"; Parameters: "/C reg delete ""HKCU\Software\Microsoft\Windows\CurrentVersion\Run"" /v LouderMe /f"; Flags: runhidden; RunOnceId: "RemoveLouderMeStartup"
