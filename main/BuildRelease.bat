
@call "c:\Program Files (x86)\Microsoft Visual Studio 9.0\VC\vcvarsall.bat" x86
msbuild boku.sln /fl1 /p:Configuration="Release" /p:Platform="x86" 
pause
