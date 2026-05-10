[Setup]
AppId={{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}
AppName=Itchy Backup
AppVersion=0.6
AppPublisher=M.Mert - Itchy Tech
DefaultDirName={autopf}\Itchy Backup
DefaultGroupName=Itchy Backup
OutputDir=..\build\output
OutputBaseFilename=ItchyBackup_v0.6_Setup
SetupIconFile=..\src\ItchyBackup\Resources\Icons\app.ico
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
UninstallDisplayIcon={app}\ItchyBackup.exe
MinVersion=10.0
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

[Languages]
Name: "turkish"; MessagesFile: "compiler:Languages\Turkish.isl"

[Tasks]
Name: "desktopicon"; Description: "Masaustu kisayolu"; GroupDescription: "Ekstra:"; Flags: unchecked

[Files]
; .NET 8 Runtime dahil tek dosya - ek bagimlilık gerekmez
Source: "..\build\publish_sc\ItchyBackup.exe"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\Itchy Backup"; Filename: "{app}\ItchyBackup.exe"
Name: "{group}\Kaldir"; Filename: "{uninstallexe}"
Name: "{autodesktop}\Itchy Backup"; Filename: "{app}\ItchyBackup.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\ItchyBackup.exe"; Description: "Itchy Backup'i baslat"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{app}"
