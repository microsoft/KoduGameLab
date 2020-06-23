#define ApplicationVersion GetFileVersion('..\Boku\bin\x86\Installer\Boku.exe')
[Setup]
AppName=Kodu Game Lab
AppVersion={#ApplicationVersion}
AppId = 055B31F9-07F8-479b-875F-F03279DF595E
AppPublisher=Microsoft Research
DefaultDirName={pf}\Microsoft Research\Kodu Game Lab
DefaultGroupName=\Microsoft Research\Kodu Game Lab
UninstallDisplayIcon={app}\Boku.exe
OutputDir=.\bin\x86\Installer\en-us
SourceDir=.\
LicenseFile=Kodu_Game_Lab_EULA_and_Code_of_Conduct.rtf
WizardImageFile=Images\InnoSideBanner100.bmp
WizardSmallImageFile=Images\InnoTopBanner100.bmp
DisableProgramGroupPage=true
DisableReadyPage=true
ShowUndisplayableLanguages=yes
OutputBaseFilename=KoduSetup{#ApplicationVersion}
SetupLogging = yes
Uninstallable = no 
DirExistsWarning=no 

[Files]
Source: "Kodu_Game_Lab_Privacy_Statement.rtf"         ; DestDir: "{app}";
Source: "Options\1F2B5B79-6EB0-45c4-A8BD-0EBDF4EE10C3.opt"; DestDir: "{app}\Options"; Tasks: checkforupdates
Source: "Options\C90D3C0E-D0B4-4aa6-B35D-0A1D9931FB38.opt"; DestDir: "{app}\Options"; Tasks: allowusageinfo

;Source: "dependencies\dotNetFx40_Full_setup.exe"; DestDir: {tmp}; Flags: deleteafterinstall; AfterInstall: InstallFramework; //Web setup version.
Source: "dependencies\dotNetFx40_Full_x86_x64.exe"; DestDir: {tmp}; Flags: deleteafterinstall; AfterInstall: InstallFramework; 
Source: "dependencies\xnafx40_redist.msi"; DestDir: {tmp}; Flags: deleteafterinstall; AfterInstall: InstallXna; 
Source: "bin\x86\Installer\en-us\KoduSetup.msi"; DestDir: {tmp}; Flags: deleteafterinstall; AfterInstall: InstallKodu; 
                                                                                                    
Source: "Images\InnoSideBanner125.bmp"; Flags: dontcopy
Source: "Images\InnoSideBanner150.bmp"; Flags: dontcopy
Source: "Images\InnoSideBanner200.bmp"; Flags: dontcopy
Source: "Images\InnoTopBanner125.bmp"; Flags: dontcopy
Source: "Images\InnoTopBanner150.bmp"; Flags: dontcopy
Source: "Images\InnoTopBanner200.bmp"; Flags: dontcopy

[Icons]
Name: "{group}\Kodu Game Lab"; Filename: "{app}\Boku.exe"
Name: "{commondesktop}\Kodu Game Lab"; Filename: "{app}\Boku.EXE"; WorkingDir: "{app}"; Tasks: desktopicon
Name: "{group}\Configure Kodu Game Lab"; Filename: "{app}\BokuPreBoot.exe"
Name: "{commondesktop}\Configure Kodu Game Lab"; Filename: "{app}\BokuPreBoot.EXE"; WorkingDir: "{app}"; Tasks: desktopicon
Name: "{group}\Uninstall Kodu Game Lab"; Filename: "{uninstallexe}"

[Registry]
Root: HKLM; Subkey: "Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION"; ValueName:"Boku.exe"; ValueData:9999; ValueType:dword; Flags: createvalueifdoesntexist;
Root: HKLM; Subkey: "Software\Wow6432Node\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION"; ValueName:"Boku.exe"; ValueData:9999; ValueType:dword; Flags: createvalueifdoesntexist;

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}";
Name: checkforupdates; Description: "{cm:CheckUpdatesResponse}"; 
Name: allowusageinfo; Description: "{cm:SendUsageReponse}"; 

[Run]
Filename: {app}\Boku.exe; Description: {cm:LaunchProgram,{cm:AppName}}; Flags: nowait postinstall skipifsilent

[CustomMessages]
AppName=Kodu

ar.CheckUpdatesResponse       =Kodu يجب التحقق من وجود تحديثات عند بدء التشغيل.
ar.SendUsageReponse           =Kodu قد ترسل معلومات الاستخدام إلى Microsoft.
ar.PrivacyStatementTitle      =بيان الخصوصية
ar.ViewPrivacyStatementMessage=بيان رأي الخصوصية
ar.NewerVersionMessage        =وهناك نسخة أحدث من Kodu في تثبيت بالفعل. يرجى إلغاء تثبيت قبل تثبيت هذا الإصدار.

cs.CheckUpdatesResponse       =Kodu kontrolovat aktualizace při spuštění.
cs.SendUsageReponse           =Kodu může poslat informace o použití společnosti Microsoft.
cs.PrivacyStatementTitle      =Prohlášení o ochraně soukromí
cs.ViewPrivacyStatementMessage=Zobrazit Prohlášení o ochraně soukromí
cs.NewerVersionMessage        =Novější verze kodu v již nainstalována. Prosím, odinstalujte před instalací této verze.

he.CheckUpdatesResponse       =Kodu צריך לבדוק אם יש עדכונים בעת האתחול.
he.SendUsageReponse           =Kodu עשוי לשלוח מידע שימוש למיקרוסופט.
he.PrivacyStatementTitle      =הצהרת פרטיות
he.ViewPrivacyStatementMessage=הצהרת פרטיות צפה
he.NewerVersionMessage        =גרסה חדשה יותר של Kodu בכבר מותקנת. הסר את ההתקנה לפני התקנת גרסה זו.

ja.CheckUpdatesResponse       =Koduは、起動時に更新を確認する必要があります。
ja.SendUsageReponse           =Koduはマイクロソフトに使用情報を送信することができます。
ja.PrivacyStatementTitle      =プライバシーに関する声明
ja.ViewPrivacyStatementMessage=ビューのプライバシーステートメント
ja.NewerVersionMessage        =中Koduの新しいバージョンがすでにインストールされています。このバージョンをインストールする前にアンインストールしてください。

ko.CheckUpdatesResponse       =Kodu 시작할 때 업데이트를 확인해야한다.
ko.SendUsageReponse           =Kodu는 Microsoft에 사용 정보를 보낼 수 있습니다.
ko.PrivacyStatementTitle      =개인 정보 보호 정책
ko.ViewPrivacyStatementMessage=보기 개인 정보 보호 정책
ko.NewerVersionMessage        =에 Kodu의 최신 버전이 이미 설치되어 있습니다. 이 버전을 설치하기 전에 제거하십시오.

lt.CheckUpdatesResponse       =Kodu turi tikrinti dėl atnaujinimų paleidžiant.
lt.SendUsageReponse           =Kodu gali siųsti naudojimo informaciją "Microsoft".
lt.PrivacyStatementTitle      =Privatumo patvirtinimas
lt.ViewPrivacyStatementMessage=Peržiūrėti Privatumo patvirtinimas
lt.NewerVersionMessage        =Naujesnis versija Kodu į jau įdiegtas (%1). Prašome pašalinti prieš diegiant šią versiją (%2).

pl.CheckUpdatesResponse       =Kodu należy sprawdzić dostępność aktualizacji przy starcie.
pl.SendUsageReponse           =Kodu może wysyłać informacje o użytkowaniu do firmy Microsoft.
pl.PrivacyStatementTitle      =Polityka prywatności
pl.ViewPrivacyStatementMessage=Oświadczenie Zobacz prywatności
pl.NewerVersionMessage        =Nowsza wersja Kodu w już zainstalowany (%1). Proszę odinstalować przed zainstalowaniem tej wersji (%2).

ru.CheckUpdatesResponse       =Коду должны проверить наличие обновлений при запуске.
ru.SendUsageReponse           =Коду можете отправить информацию об использовании в корпорацию Майкрософт.
ru.PrivacyStatementTitle      =Заявление о конфиденциальности
ru.ViewPrivacyStatementMessage=Посмотреть Заявление о конфиденциальности
ru.NewerVersionMessage        =Вышла новая версия Kodu в уже установлен (%1). Пожалуйста, удалите перед установкой этой версии (%2).

tr.CheckUpdatesResponse       =Kodu başlangıçta güncellemeleri kontrol etmelidir.
tr.SendUsageReponse           =Kodu Microsoft'a kullanım bilgilerini gönderebilir.
tr.PrivacyStatementTitle      =Gizlilik Beyanı
tr.ViewPrivacyStatementMessage=Görünüm Gizlilik
tr.NewerVersionMessage        =Içinde Kodu yeni bir sürümü zaten (%1) yüklü. Bu sürümü (%2) yüklemeden önce kaldırın.

zh.CheckUpdatesResponse       =Kodu应该检查在启动时更新。
zh.SendUsageReponse           =Kodu可以发送使用信息给微软。
zh.PrivacyStatementTitle      =隐私声明
zh.ViewPrivacyStatementMessage=查看隐私声明
zh.NewerVersionMessage        =已安装Kodu的新版本。安装此版本之前，请卸载。

zhTW.CheckUpdatesResponse       =Kodu應該檢查在啟動時更新。
zhTW.SendUsageReponse           =Kodu可以發送使用信息給微軟。
zhTW.PrivacyStatementTitle      =隱私聲明
zhTW.ViewPrivacyStatementMessage=查看隱私聲明
zhTW.NewerVersionMessage        =已安裝Kodu的新版本。安裝此版本之前，請卸載。

[Languages]
Name: en; MessagesFile: "compiler:Default.isl,InnoLoc\Custom\english.isl"
Name: cs; MessagesFile: "compiler:Languages\Czech.isl"
;Name: cy; MessagesFile: "compiler:Languages\Welsh.isl,InnoLoc\Custom\Welsh.isl"
Name: el; MessagesFile: "compiler:Languages\Greek.isl,InnoLoc\Custom\Greek.isl"
Name: es; MessagesFile: "compiler:Languages\Spanish.isl,InnoLoc\Custom\Spanish.isl"
Name: he; MessagesFile: "compiler:Languages\Hebrew.isl"
Name: it; MessagesFile: "compiler:Languages\Italian.isl,InnoLoc\Custom\Italian.isl"
Name: nl; MessagesFile: "compiler:Languages\Dutch.isl,InnoLoc\Custom\Dutch.isl"
Name: pl; MessagesFile: "compiler:Languages\Polish.isl"
Name: pt; MessagesFile: "compiler:Languages\Portuguese.isl,InnoLoc\Custom\Portuguese.isl"
Name: ru; MessagesFile: "compiler:Languages\Russian.isl"
Name: tr; MessagesFile: "compiler:Languages\Turkish.isl"
Name: de; MessagesFile: "compiler:Languages\German.isl,InnoLoc\Custom\German.isl"
Name: fr; MessagesFile: "compiler:Languages\French.isl,InnoLoc\Custom\French.isl"
Name: ja; MessagesFile: "compiler:Languages\Japanese.isl"

Name: ar; MessagesFile: "InnoLoc\Arabic.isl"
Name: hu; MessagesFile: "InnoLoc\Hungarian.isl,InnoLoc\Custom\Hungarian.isl"
Name: is; MessagesFile: "InnoLoc\Icelandic.isl,InnoLoc\Custom\Icelandic.isl"
Name: lt; MessagesFile: "InnoLoc\Lithuanian.isl"
Name: ko; MessagesFile: "InnoLoc\Korean.isl"
Name: "zh"; MessagesFile: "InnoLoc\ChineseSimplified.isl"
Name: "zhTW"; MessagesFile: "InnoLoc\ChineseTraditional.isl"
Name: no; MessagesFile: "compiler:Languages\Norwegian.isl"

[Code]

function GetScalingFactor: Integer;
begin
  if WizardForm.Font.PixelsPerInch >= 192 then Result := 200
    else
  if WizardForm.Font.PixelsPerInch >= 144 then Result := 150
    else
  if WizardForm.Font.PixelsPerInch >= 120 then Result := 125
    else Result := 100;
end;

procedure LoadEmbededScaledBitmap(Image: TBitmapImage; NameBase: string);
var
  Name: String;
  FileName: String;
begin
  Name := Format('%s%d.bmp', [NameBase, GetScalingFactor]);
  ExtractTemporaryFile(Name);
  FileName := ExpandConstant('{tmp}\' + Name);
  Image.Bitmap.LoadFromFile(FileName);
  DeleteFile(FileName);
end;

procedure InstallFramework;
var
  StatusText: string;
  ResultCode: Integer;
begin
  StatusText := WizardForm.StatusLabel.Caption;
  WizardForm.StatusLabel.Caption := SetupMessage(msgWizardInstalling) + ' .NET framework...';
  WizardForm.ProgressGauge.Style := npbstMarquee;
  try
    if not RegKeyExists(HKEY_LOCAL_MACHINE, 'Software\Microsoft\.NETFramework\policy\v4.0') then
    begin
      if not ShellExec('',ExpandConstant('{tmp}\dotNetFx40_Full_x86_x64.exe'), '/q /norestart', '', SW_SHOWNORMAL, ewWaitUntilTerminated, ResultCode) then
      begin
        //the installation failed
        MsgBox('.NET installation failed with code: ' + IntToStr(ResultCode) + '.',
          mbError, MB_OK);
        Abort()
      end;
    end;
  finally
    WizardForm.StatusLabel.Caption := StatusText;
    WizardForm.ProgressGauge.Style := npbstNormal;
  end;
end;

procedure InstallXna;
var
  StatusText: string;
  ResultCode: Integer;
begin
  StatusText := WizardForm.StatusLabel.Caption;
  WizardForm.StatusLabel.Caption := SetupMessage(msgWizardInstalling) + ' XNA framework...';
  WizardForm.ProgressGauge.Style := npbstMarquee;

  try
    if not RegKeyExists(HKEY_LOCAL_MACHINE, 'Software\Microsoft\XNA\Framework\v4.0') then
    begin
      if not ShellExec('', 'msiexec',ExpandConstant('/I "{tmp}\xnafx40_redist.msi" /q'),'', SW_SHOWNORMAL, ewWaitUntilTerminated, ResultCode) then
      begin
        MsgBox('XNA installation failed with code: ' + IntToStr(ResultCode) + '.', mbError, MB_OK);
        Abort()
      end;
    end;
  finally
    WizardForm.StatusLabel.Caption := StatusText;
    WizardForm.ProgressGauge.Style := npbstNormal;
  end;
end;

procedure InstallKodu;
var
  StatusText: string;
  ResultCode: Integer;
begin
  StatusText := WizardForm.StatusLabel.Caption;
  WizardForm.StatusLabel.Caption := SetupMessage(msgWizardInstalling) + ' Kodu...';
  WizardForm.ProgressGauge.Style := npbstMarquee;
  //StatusText := GetFileVersion('{tmp}\KoduSetup.msi')

  try
    if not ShellExec('', 'msiexec',ExpandConstant('/I "{tmp}\KoduSetup.msi" TARGETDIR="{app}" APPLICATIONFOLDER="{app}" /q'),'', SW_SHOWNORMAL, ewWaitUntilTerminated, ResultCode) then
    begin
      MsgBox('Kodu installation failed with code: ' + IntToStr(ResultCode) + '.',
        mbError, MB_OK);
      Abort()
    end;
  finally
    WizardForm.StatusLabel.Caption := StatusText;
    WizardForm.ProgressGauge.Style := npbstNormal;
  end;
end;

/////////////////////////////////////////////////////////////////////
// Procedure to split a string into an array of integers 
procedure Explode(var Dest: TArrayOfInteger; Text: String; Separator: String);
var                          
  i, p: Integer;
begin
  i := 0;
  repeat
    SetArrayLength(Dest, i+1);
    p := Pos(Separator,Text);
    if p > 0 then begin
      Dest[i] := StrToInt(Copy(Text, 1, p-1));
      Text := Copy(Text, p + Length(Separator), Length(Text));
      i := i + 1;
    end else begin
      Dest[i] := StrToInt(Text);
      Text := '';
    end;
  until Length(Text)=0;
end;

// Function compares version strings numerically:
//     * when v1 = v2, result = 0  
//     * when v1 &lt; v2, result = -1  
//     * when v1 &gt; v2, result = 1
//
// Supports version numbers with trailing zeroes, for example 1.02.05.
// Supports comparison of two version number of different lengths, for example
//     CompareVersions('1.2', '2.0.3')
// When any of the parameters is '' (empty string) it considers version number as 0
function CompareVersions(v1: String; v2: String): Integer;
var
  v1parts: TArrayOfInteger;
  v2parts: TArrayOfInteger;
  i: Integer;
begin
  if v1 = '' then
  begin
    v1 := '0';
  end;

  if v2 = '' then
  begin
    v2 := '0';
  end;

  Explode(v1parts, v1, '.');
  Explode(v2parts, v2, '.');
  
  if (GetArrayLength(v1parts) > GetArrayLength(v2parts)) then
  begin
    SetArrayLength(v2parts, GetArrayLength(v1parts)) 
  end else if (GetArrayLength(v2parts) > GetArrayLength(v1parts)) then
  begin
    SetArrayLength(v1parts, GetArrayLength(v2parts)) 
  end; 
  
  for i := 0 to GetArrayLength(v1parts) - 1 do 
  begin
    if v1parts[i] > v2parts[i] then
    begin
      { v1 is greater }
      Result := 1;
      exit;
    end else if v1parts[i] < v2parts[i] then
    begin
      { v2 is greater }
      Result := -1;
      exit;
    end;
  end;
  
  { Are Equal }
  Result := 0;
end;

#ifdef UNICODE
  #define AW "W"
#else
  #define AW "A"
#endif

const
  ERROR_SUCCESS = $00000000;
  ERROR_NOT_ENOUGH_MEMORY = $00000008;
  ERROR_INVALID_PARAMETER = $00000057;
  ERROR_NO_MORE_ITEMS = $00000103;
  ERROR_BAD_CONFIGURATION = $0000064A;

function MsiEnumRelatedProducts(lpUpgradeCode: string; dwReserved: DWORD;
  iProductIndex: DWORD; lpProductBuf: string): UINT;
  external 'MsiEnumRelatedProducts{#AW}@msi.dll stdcall';

function MsiGetProductInfo(
    szProduct: string;
    szProperty: string;
    lpValueBuf: string; 
    var pcchValueBuf: DWORD): UINT;
    external 'MsiGetProductInfo{#AW}@msi.dll stdcall';

/////////////////////////////////////////////////////////////////////
//Check to be sure we are not trying to install an old version over
//a newer one.
function CheckVersion(): Integer;
var
  I: Integer;
  ProductBuf: string;
  ValueBuf: string;
  ValueLen: DWORD;
begin
  Result := 0;

  SetLength(ProductBuf, 39);
  I := 0;                                
  while MsiEnumRelatedProducts('{055B31F9-07F8-479b-875F-F03279DF595E}', 0, I, ProductBuf) = ERROR_SUCCESS do
  begin
    SetLength(ValueBuf, 256);
    ValueLen := 256;
    Result := MsiGetProductInfo(ProductBuf,'VersionString',ValueBuf,ValueLen);
    SetLength(ValueBuf, ValueLen);

    if CompareVersions(ValueBuf, '{#SetupSetting("AppVersion")}') = -1 then
    begin
      //MsgBox('Older version installed', mbInformation, MB_OK);
    end;
    if CompareVersions(ValueBuf, '{#SetupSetting("AppVersion")}') = 0 then
    begin
      //MsgBox('Same version installed', mbInformation, MB_OK);
    end;
    if CompareVersions(ValueBuf, '{#SetupSetting("AppVersion")}') = 1 then
    begin
      MsgBox(FmtMessage(CustomMessage('NewerVersionMessage'), [ValueBuf, '{#SetupSetting("AppVersion")}']), mbInformation, MB_OK);
      Abort()
    end;
    I := I+1;             
  end;
end;

/////////////////////////////////////////////////////////////////////
procedure CurStepChanged(CurStep: TSetupStep);
var
  lang:string;
begin
  if  CurStep=ssPostInstall then
    begin

      //Translate Chinese codes into the format Kodu wants. 
      case ExpandConstant('{language}') of
        'zh': lang := 'zh-CN'; 
        'zhTW': lang := 'zh-TW';
        else lang := ExpandConstant('{language}'); 
      end

      //Save installed language so client can use it.
      SaveStringToFile(ExpandConstant('{app}\InstallerLanguage.txt'), lang, False);
    end
end;

var
  //Privacy statement UI.
  LGPLPage: TOutputMsgMemoWizardPage;
  ViewPrivacyButton: TButton;

procedure ViewPrivacyButtonClick(Sender: TObject);
var WordpadLoc: String;
    RetCode: Integer;
begin
  RegQueryStringValue(HKLM, 'SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\WORDPAD.EXE', '', WordpadLoc);

  // on NT/2000 it's a REG_EXPAND_SZ, so expand constant ProgramFiles
  StringChange(WordpadLoc, '%ProgramFiles%', ExpandConstant('{pf}'));
  // remove " at begin and end pf string
  StringChange(WordpadLoc, '"', '');

  try
    ExtractTemporaryFile('Kodu_Game_Lab_Privacy_Statement.rtf')
  except
    MsgBox('Cannot extract license file.', mbError, mb_Ok);
  end;

  if not Exec(WordpadLoc, '"' + ExpandConstant('{tmp}\Kodu_Game_Lab_Privacy_Statement.rtf') + '"', '', SW_SHOW, ewNoWait, RetCode) then
    MsgBox('Cannot display license file.', mbError, mb_Ok);
end;

procedure InitializeWizard();
begin
	//Log(Format('ScalingFactor: %d', [GetScalingFactor]));
	//Log(Format('PixelsPerInch: %d', [WizardForm.Font.PixelsPerInch]));
	//Log(Format('ScaleX(1): %d', [ScaleX(1)]));

	if GetScalingFactor > 100 then
	begin
		LoadEmbededScaledBitmap(WizardForm.WizardBitmapImage, 'InnoSideBanner');
		LoadEmbededScaledBitmap(WizardForm.WizardBitmapImage2, 'InnoSideBanner');
		LoadEmbededScaledBitmap(WizardForm.WizardSmallBitmapImage, 'InnoTopBanner');
	end;

    ViewPrivacyButton := TButton.Create(WizardForm.LicenseMemo.Parent);
    ViewPrivacyButton.Caption := CustomMessage('ViewPrivacyStatementMessage');
    ViewPrivacyButton.OnClick := @ViewPrivacyButtonClick;
    ViewPrivacyButton.Parent := WizardForm.LicenseAcceptedRadio.Parent;

    ViewPrivacyButton.Left :=WizardForm.InfoAfterPage.Left + ScaleX(20);
    ViewPrivacyButton.Top := WizardForm.InfoAfterPage.Height +ScaleY(90);
    ViewPrivacyButton.Width := ScaleX(210); 
    ViewPrivacyButton.Height := ScaleY(ViewPrivacyButton.Height); 
    ViewPrivacyButton.Parent := WizardForm.NextButton.Parent;
end;

procedure CurPageChanged(CurPage: Integer);
begin
  ViewPrivacyButton.Visible := CurPage = wpLicense;
end;

function InitializeSetup: Boolean;
var
  I: Integer;
  ProductBuf: string;
  iResultCode: Integer;
begin
  Result := True;
  CheckVersion()
end;