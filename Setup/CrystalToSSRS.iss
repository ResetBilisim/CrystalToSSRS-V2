; Inno Setup script for CrystalToSSRS WinForms application
; Requires Inno Setup Compiler (iscc.exe)
; Build: iscc Setup\CrystalToSSRS.iss

[Setup]
AppId={{9F0B0F6C-8C7F-4D2F-A46A-CRSSRS2025}}
AppName=CrystalToSSRS Converter
AppVersion=1.0.0
AppPublisher=Reset Bilişim
AppPublisherURL=https://github.com/ResetBilisim/CrystalToSSRS-V2
DefaultDirName={pf64}\CrystalToSSRS
DefaultGroupName=CrystalToSSRS
DisableDirPage=no
DisableProgramGroupPage=no
LicenseFile=..\README_FIRST.md
OutputDir=..
OutputBaseFilename=CrystalToSSRS_Setup
Compression=lzma
SolidCompression=yes
ArchitecturesInstallIn64BitMode=x64
DisableWelcomePage=no
SetupLogging=yes

[Languages]
Name: "tr"; MessagesFile: "compiler:Languages/Turkish.isl"
Name: "en"; MessagesFile: "compiler:Default.isl"

[Files]
; Main application binaries (after you build Debug/Release the exe and dlls reside in bin/Release)
Source: "..\bin\Release\CrystalToSSRS.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\bin\Release\CrystalToSSRS.exe.config"; DestDir: "{app}"; Flags: ignoreversion overwritereadonly
Source: "..\license_keys.json"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\lic_keys.txt"; DestDir: "{app}"; Flags: ignoreversion
; Optional: any Crystal runtime dependencies (add manually if needed)
; Source: "..\Dependencies\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Dirs]
Name: "{app}\Logs"; Flags: uninsalwaysuninstall

[Icons]
Name: "{group}\CrystalToSSRS"; Filename: "{app}\CrystalToSSRS.exe"
Name: "{group}\Kaldır (Uninstall)"; Filename: "{uninstallexe}"
Name: "{userdesktop}\CrystalToSSRS"; Filename: "{app}\CrystalToSSRS.exe"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Masaüstü kısayolu oluştur"; GroupDescription: "Ekstra Görevler:"; Flags: unchecked

[Run]
; Run application after install (optional, unchecked by default)
Filename: "{app}\CrystalToSSRS.exe"; Description: "Uygulamayı başlat"; Flags: nowait postinstall unchecked

[UninstallDelete]
Type: files; Name: "{app}\license_state.json"

[Code]
// Ensure .NET Framework 4.8 present (simple check)
function IsDotNet48Installed(): Boolean;
var
  success: Boolean;
  value: Cardinal;
begin
  success := RegQueryDWordValue(HKLM, 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full', 'Release', value);
  // Release key for .NET 4.8 and above is >= 528040
  if success and (value >= 528040) then Result := True else Result := False;
end;

function InitializeSetup(): Boolean;
begin
  if not IsDotNet48Installed() then
  begin
    MsgBox('Bu uygulama .NET Framework 4.8 gerektirir. Lütfen önce yükleyin.', mbError, MB_OK);
    Result := False;
  end
  else
    Result := True;
end;

[CustomMessages]
tr.DotNetMissing=.NET Framework 4.8 bulunamadı. Önce kurulum yapın.
