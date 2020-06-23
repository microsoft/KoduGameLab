
# w/ bat file installing XNA and Kodu, both from MSIs.
DesktopAppConverter.exe -Installer .\DACInstall.bat -AppExecutable boku.exe -Destination ..\BokuSetup\bin\x86\Installer\en-us -PackageName "Microsoft.Kodu" -PackageDisplayName "Kodu"  -Publisher "CN=Microsoft Corporation, O=Microsoft Corporation, L=Redmond, S=Washington, C=US" -PackagePublisherDisplayName "Microsoft Studios" -Version (Get-Item ../Boku/bin/x86/installer/Boku.exe).VersionInfo.FileVersion -MakeAppx -Verbose -Sign -Verify

