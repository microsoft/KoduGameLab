
setlocal enableextensions enabledelayedexpansion

REM ---------------------------------------------------
REM  PULL IN COMMAND LINE ARGUMENTS

set SolutionDir=%1
set SolutionName=%2
set OutDir=%3
set ConfigurationName=%4
set ProjectDir=%5

REM ---------------------------------------------------
REM  STRIP QUOTES FROM ARGUMENTS

set SolutionDir=!SolutionDir:~1,-1!
set SolutionName=!SolutionName:~1,-1!
set OutDir=!OutDir:~1,-1!
set ConfigurationName=!ConfigurationName:~1,-1!
set ProjectDir=!ProjectDir:~1,-1!

REM ---------------------------------------------------
REM  SETUP SOME PATHS

set PathToWix=%WIX%\bin
set PathToBokuOutput=%SolutionDir%%SolutionName%\%OutDir%

echo Boku Project Output: %PathToBokuOutput%

REM ---------------------------------------------------
REM  COPY BOKU PROJECT OUTPUT

REM scoy hack:  There appears to be something messed up with the configuration of the installer build.
REM				It looks like it's configured as an x86 build but this cmd file isn't.
REM				Note it's also now building into Release\en-us rather than x86\Installer\en-us.  No clue why. 
REM set OutDir=bin\x86\Release\
REM set PathToBokuOutput=%SolutionDir%%SolutionName%\%OutDir%


echo ========
echo ProjectDir %ProjectDir%
echo SolutionDir %SolutionDir%
echo SolutionName %SolutionName%
echo ConfigurationName %ConfigurationName%
echo OutDir %OutDir%
echo PathToBokuOutput %PathToBokuOutput%
echo ========

echo *** removing ApplicationSource : %ProjectDir%\ApplicationSource
if EXIST "%ProjectDir%\ApplicationSource" ( rmdir /s /q "%ProjectDir%\ApplicationSource" )
if EXIST "%ProjectDir%\SourceDir" ( rmdir /s /q "%ProjectDir%\SourceDir" )

REM I've added the /R option to prevent the build process from failing when it attempts to overwrite a READ-ONLY file. PV
REM
REM xcopy /i /y /R /exclude:..\..\..\excludes.txt "%PathToBokuOutput%*.*" "%ProjectDir%\ApplicationSource"
echo xcopy /i /y /R /exclude:%SolutionDir%BokuSetup\excludes.txt "%PathToBokuOutput%*.*" "%ProjectDir%\ApplicationSource"
     xcopy /i /y /R /exclude:%SolutionDir%BokuSetup\excludes.txt "%PathToBokuOutput%*.*" "%ProjectDir%\ApplicationSource"
xcopy /i /y /e /R "%PathToBokuOutput%\Content\*.*" "%ProjectDir%\SourceDir\Content"
xcopy /i /y /e /R "%PathToBokuOutput%\ContentHiDef\*.*" "%ProjectDir%\SourceDir\ContentHiDef"

REM ---------------------------------------------------
REM  BUILD BOKU CONTENT WIX FRAGMENT

echo "%SolutionDir%BokuSetupTool\bin\x86\%ConfigurationName%\BokuSetupTool.exe" /Spawned /SolutionDir %SolutionDir% /Command CheckoutContentWxs
"%SolutionDir%BokuSetupTool\bin\x86\%ConfigurationName%\BokuSetupTool.exe" /Spawned /SolutionDir %SolutionDir% /Command CheckoutContentWxs

REM Since we don't have access the MS source control, we just make the Content.wxs file writeable on the local machine. PV
REM We again have source control access to Content.wxs. Commenting this out again. @eanders.
REM attrib -R "%ProjectDir%\Content.wxs"

echo "%PathToWix%\heat.exe" dir "%ProjectDir%\SourceDir" -v -dr APPLICATIONFOLDER -cg BokuContentGroup -gg -srd -ke -template fragment -o "%ProjectDir%\Content.wxs"
"%PathToWix%\heat.exe" dir "%ProjectDir%\SourceDir" -v -dr APPLICATIONFOLDER -cg BokuContentGroup -gg -srd -ke -template fragment -o "%ProjectDir%\Content.wxs"
