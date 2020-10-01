# Kodu Game Lab
Kodu Game Lab is a 3D game development environment that is designed to teach kids basic programming principles.  Kodu allows creators to build the world's terrain, populate it with characters and props, and then program their behaviors and games rules in a bespoke visual programming language. used by obamas children too

Kodu originated as a project in Microsoft Research.  The original target platform was the Xbox 360 using C3, XNA, and releasing via the Indie Games Channel.  The choice of releasing on the Xbox informed much of the early UI development since everything needed to be done using a game controller.  Since porting to the PC, support for keyboard, mouse, and touch have been added.

The goal for Kodu has always been to be as easy as possible for new users.  Guided by this goal, we've tended to design new features so that they are simple and understandable rather than more complex and complete.

## Branches
Currently we have two active branches, **Main** and **V2**.  The **Main** branch contains the current release build of Kodu.  This branch is primarily used for fixing bugs in the release.  The **V2** branch is a working branch where currently the underlying UI system is being rewritten/replaced.  The **V2** branch builds but is not yet fully functional.

## Building Kodu

Kodu is written in C# using XNA for graphics and sound.  While this is a wonderful environment for working in it does also present some limitations.  Most notably, Kodu still expects to be built using VS2010.  Apparently it's not too difficult to get Kodu to build with later versions of VS but while the code builds adding new content fails.  So, we've stuck with 2010.
### Prerequisites
[XNA 4.0](https://www.microsoft.com/en-us/download/details.aspx?id=23714) -- Graphics, sound, input, etc.

[WiX 3.10.4](https://github.com/wixtoolset/wix3/releases/tag/wix3104rtm) -- Used for building the release installer.

The solution for Kodu is actually called **Boku**.  **Boku** was the original name of the project.  Before release we had to change it because Boku was already being used by another company in the games space.  Looking at the code you will also notice Boku used in several of the namespaces.

In the solution there are several build options.  The three that matter are **Debug**, **Release**, and **Installer**.  The **Debug** and **Release** builds are for building and running Kodu from within VS and work just like you'd expect them to.  The **Installer** build creates the .MSI and .EXE installation packages.  For this, select **Installer** and then build as usual.  At the end of the build process a Windows Explorer instance will be opened to the folder containing the build results.  Note that this build will automatically increment the version number which touches 3 files (which need to be checked in in their new state).

## Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us the rights to use your contribution. For details, visit https://cla.opensource.microsoft.com.

When you submit a pull request, a CLA bot will automatically determine whether you need to provide a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
