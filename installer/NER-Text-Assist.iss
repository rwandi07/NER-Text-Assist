#define MyAppName "NER Text Assist"
#define MyAppVersion "0.1.0"
#define MyAppPublisher "NER"
#define MyAppExeName "NER.TextAssist.exe"

[Setup]
; IMPORTANT: AppId is permanent. Never change it in future releases.
AppId={{DCA82FC0-4F1F-4B40-9D19-54FB35F6C7A1}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={localappdata}\Programs\NER Text Assist
DefaultGroupName=NER Text Assist
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
OutputDir=output
OutputBaseFilename=NER-Text-Assist-Setup-{#MyAppVersion}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
CloseApplications=yes
RestartApplications=yes
UninstallDisplayIcon={app}\{#MyAppExeName}
SetupLogging=yes

[Files]
Source: "..\artifacts\publish\win-x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\NER Text Assist"; Filename: "{app}\{#MyAppExeName}"

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Jalankan NER Text Assist"; Flags: nowait postinstall skipifsilent
