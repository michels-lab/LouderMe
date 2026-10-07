#ifndef MyAppVersion
  #define MyAppVersion "0.1.2"
#endif
#ifndef PublishDir
  #define PublishDir "..\artifacts\publish"
#endif

[Setup]
AppId={{A3F87C16-D4F4-4B5D-A05D-49AE50245921}
AppName=LouderMe
AppVerName=LouderMe v{#MyAppVersion}
AppVersion={#MyAppVersion}
AppPublisher=Michel's Lab
AppPublisherURL=https://github.com/realmichelduarte
AppSupportURL=https://github.com/realmichelduarte/michel-s-life-releases/releases
AppUpdatesURL=https://github.com/realmichelduarte/michel-s-life-releases/releases
AppContact=michelslab.dev@gmail.com
VersionInfoCompany=Michel's Lab
VersionInfoDescription=LouderMe Desktop — Sound That Lifts You
VersionInfoProductName=LouderMe Desktop
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
PrivilegesRequired=admin
PrivilegesRequiredOverridesAllowed=dialog
SetupIconFile=..\src\LouderMeDesktop\Assets\LouderMe.ico
UninstallDisplayIcon={app}\LouderMe.exe
SetupLogging=yes
CloseApplications=yes
RestartApplications=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"

[CustomMessages]
english.DesktopShortcut=Create a desktop shortcut
spanish.DesktopShortcut=Crear un acceso directo en el escritorio
english.StartupOption=Start LouderMe with Windows
spanish.StartupOption=Iniciar LouderMe con Windows
english.AdditionalOptions=Additional options:
spanish.AdditionalOptions=Opciones adicionales:
english.LaunchApp=Launch LouderMe
spanish.LaunchApp=Abrir LouderMe

[Files]
Source: "{#PublishDir}\LouderMe.exe"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\LouderMe"; Filename: "{app}\LouderMe.exe"; IconFilename: "{app}\LouderMe.exe"
Name: "{autodesktop}\LouderMe"; Filename: "{app}\LouderMe.exe"; IconFilename: "{app}\LouderMe.exe"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "{cm:DesktopShortcut}"; GroupDescription: "{cm:AdditionalOptions}"; Flags: unchecked
Name: "startup"; Description: "{cm:StartupOption}"; GroupDescription: "{cm:AdditionalOptions}"; Flags: unchecked

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "LouderMe"; ValueData: """{app}\LouderMe.exe"" --startup"; Tasks: startup; Flags: uninsdeletevalue

[Run]
Filename: "{app}\LouderMe.exe"; Description: "{cm:LaunchApp}"; Flags: nowait postinstall skipifsilent
