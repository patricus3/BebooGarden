; Installer for Beboo Garden: Enhanced Edition.
;
; Build it with:
;   dotnet publish BebooGarden\BebooGarden.csproj -c Release -r win-x64 --self-contained true ^
;       -p:EnableMGCBItems=false -o installer\payload
;   "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" installer\BebooGarden.iss
;
; PayloadDir and OutDir can both be pointed elsewhere without editing this file, e.g.
;   ISCC.exe /DPayloadDir="D:\build\payload" /DOutDir="%USERPROFILE%\Desktop" installer\BebooGarden.iss
;
; Two decisions worth knowing about:
;
; It installs per user, not into Program Files. The game keeps save.dat and crash.log beside its
; own executable and reads mods from a folder there, none of which a normal account may write to
; under Program Files. PrivilegesRequired=lowest with {autopf} lands it in
; %LocalAppData%\Programs instead, which is writable, and asks for no administrator prompt.
;
; The payload is self contained, so there is no .NET runtime to install first. That costs size,
; which is worth it for a game whose players should not have to go and find a prerequisite.

#define AppName "Beboo Garden: Enhanced Edition"
#define AppShortName "Beboo Garden"
#define AppVersion "2.1.0"
#define AppPublisher "Saladeuh"
#define AppURL "https://github.com/Saladeuh/BebooGarden"
#define AppExe "BebooGarden.exe"

#ifndef PayloadDir
  #define PayloadDir "payload"
#endif
#ifndef OutDir
  #define OutDir "output"
#endif

[Setup]
AppId={{8E6A0F2C-4B1D-4D2A-9A3E-7C51B0E6D914}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppURL}
AppSupportURL={#AppURL}
AppUpdatesURL={#AppURL}
VersionInfoVersion={#AppVersion}
VersionInfoProductName={#AppName}

; Per user: see the note at the top about save.dat needing a writable folder.
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
DefaultDirName={autopf}\{#AppShortName}
DefaultGroupName={#AppShortName}
DisableProgramGroupPage=yes
AllowNoIcons=yes

OutputDir={#OutDir}
OutputBaseFilename=BebooGarden-EnhancedEdition-v{#AppVersion}-setup
SetupIconFile=..\BebooGarden\Icon.ico
UninstallDisplayIcon={app}\{#AppExe}
UninstallDisplayName={#AppName}

Compression=lzma2/max
SolidCompression=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

; A blind player is the expected user. The classic wizard is the one screen readers handle best,
; and every page it can skip is a page nobody has to tab through.
WizardStyle=classic
ShowLanguageDialog=auto

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "french"; MessagesFile: "compiler:Languages\French.isl"
Name: "german"; MessagesFile: "compiler:Languages\German.isl"
Name: "polish"; MessagesFile: "compiler:Languages\Polish.isl"
Name: "brazilianportuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
; Everything the publish produced, mods folder and its documentation included.
Source: "{#PayloadDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#AppShortName}"; Filename: "{app}\{#AppExe}"
Name: "{group}\{cm:UninstallProgram,{#AppShortName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#AppShortName}"; Filename: "{app}\{#AppExe}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExe}"; Description: "{cm:LaunchProgram,{#AppShortName}}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; save.dat, crash.log and anything the player dropped into mods are deliberately not listed here.
; Uninstalling should not take somebody's beboos away, so the folder stays if it still holds them.
Type: dirifempty; Name: "{app}\mods"
Type: dirifempty; Name: "{app}"
