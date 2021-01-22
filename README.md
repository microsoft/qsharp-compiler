# Microsoft Quantum Development Kit: <br>Q# Compiler and Language Server #

Welcome to the Microsoft Quantum Development Kit!

This repository contains the Q# compiler included in the [Quantum Development Kit](https://docs.microsoft.com/quantum/),
as well as the Q# language server included in our [Visual Studio extension](https://marketplace.visualstudio.com/items?itemName=quantum.DevKit) and our [Visual Studio Code extension](https://marketplace.visualstudio.com/items?itemName=quantum.quantum-devkit-vscode).
For more information related to the language server protocol take a look at [this repository](https://github.com/Microsoft/language-server-protocol).
These extensions provide the IDE integration for Q#, and can be found on this repository as well.

The Q# compiler provides a [command line interface](./src/QsCompiler/CommandLineTool). For further information on how to use Q# binaries take a look at the [README](./src/QsCompiler/CommandLineTool/README.md) in that folder.

- **[QsCompiler](./src/QsCompiler/)**: Q# compiler including the command line tool
- **[QsCompiler/LanguageServer](./src/QsCompiler/LanguageServer/)**: Q# language server
- **[VSCodeExtension](./src/VSCodeExtension/)**: Visual Studio Code extension
- **[VisualStudioExtension](./src/VisualStudioExtension/)**: Visual Studio extension

## New to Quantum? ##

See the [introduction to quantum computing](https://docs.microsoft.com/quantum/concepts/) provided with the Quantum Development Kit.

## Installing the Quantum Development Kit

**If you're looking to use Q# to write quantum applications, please see the instructions on how to get started with using the [Quantum Development Kit](https://docs.microsoft.com/quantum/install-guide/) including the Q# compiler, language server, and development environment extensions.**

Please see the [installation guide](https://docs.microsoft.com/quantum/install-guide) for further information on how to get started using the Quantum Development Kit to develop quantum applications.
You may also visit our [Quantum](https://github.com/microsoft/quantum) repository, which offers a wide variety of samples on how to write quantum based programs.

## Building from Source ##

Before you can build the source code on this repository and start contributing to the Q# compiler and extensions you need to run the PowerShell script [bootstrap.ps1](./bootstrap.ps1) to set up your environment.
We refer to the [PowerShell GitHub repository](https://github.com/powershell/powershell) for instructions on how to install PowerShell.
The script in particular generates the files that are needed for building based on the templates in this repository.

The Q# compiler and language server in this repository are built using [.NET Core](https://docs.microsoft.com/dotnet/core/).
For instructions on how to build and debug the Visual Studio Code extension take a look at [this file](./src/VSCodeExtension/BUILDING.md).
Building and debugging the Visual Studio extension requires Visual Studio 2019. Open [the corresponding solution](./VisualStudioExtension.sln) and set the [QSharpVsix project](./src/VisualStudioExtension/QSharpVsix/) as startup project, then launch and debug the extension as usual.
The Visual Studio extension is built on the [.NET Framework 4.7.2](https://dotnet.microsoft.com/download/dotnet-framework/net472) that comes with Visual Studio 2019. Alternatively you can easily obtain it via the Visual Studio Installer.

We recommend uninstalling any other Q# extensions when working on the extensions in this repository.

### Tips for using VSCode ###
This repository includes both C# and F# code, as well as .csproj and .fsproj projects organizing that code. The recommended extensions for interacting with these language types are the [Microsoft C# extension powered by OmniSharp](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csharp) and the [Ionide FSharp extension](https://marketplace.visualstudio.com/items?itemName=Ionide.Ionide-fsharp). Several of the projects in each language express dependencies on the other language, which can cause errors resolving namespaces even when the builds succeed without errors. To resolve these errors in C# projects that depend on F# resources, ensure the the MSBuild utilized by Omnisharp comes from an install of Visual Studio or Visual Studio Community edition with support for F# installed. To resolve errors loading .csproj files in the Ionide extension, use the "Change Workspace or Solution" option in the F#: Solution Explorer to select the top level "qsharp-compiler" folder. This will allow Ionide to find only the .fsproj projects instead of trying to load both .csproj and .fsproj listed in the solution files.

## Build Status ##

| branch | status    |
|--------|-----------|
| main | [![Build Status](https://dev.azure.com/ms-quantum-public/Microsoft%20Quantum%20(public)/_apis/build/status/microsoft.qsharp-compiler?branchName=main)](https://dev.azure.com/ms-quantum-public/Microsoft%20Quantum%20(public)/_build/latest?definitionId=14&branchName=main) |

## Feedback ##

If you have feedback about the content in this repository, please let us know by filing a [new issue](https://github.com/microsoft/qsharp-compiler/issues/new/choose)!
If you have feedback about some other part of the Microsoft Quantum Development Kit, please see the [contribution guide](https://docs.microsoft.com/quantum/contributing/) for more information.

## Reporting Security Issues ##

Security issues and bugs should be reported privately, via email, to the Microsoft Security
Response Center (MSRC) at [secure@microsoft.com](mailto:secure@microsoft.com). You should
receive a response within 24 hours. If for some reason you do not, please follow up via
email to ensure we received your original message. Further information, including the
[MSRC PGP](https://technet.microsoft.com/en-us/security/dn606155) key, can be found in
the [Security TechCenter](https://technet.microsoft.com/en-us/security/default).

## Legal and Licensing ##

### Telemetry ###

By default, sending out telemetry is disabled for all code in this repository, but it can be enabled via compilation flag.
Our shipped extensions that are built based on the code in this repository support collecting telemetry.
In that case, opt-in or opt-out works via the corresponding setting in Visual Studio and Visual Studio Code,
and the telemetry we collect falls under the [Microsoft Privacy Statement](https://privacy.microsoft.com/privacystatement).

### Data Collection ###

The software may collect information about you and your use of the software and send it to Microsoft. Microsoft may use this information to provide services and improve our products and services. You may turn off the telemetry as described in the repository. There are also some features in the software that may enable you and Microsoft to collect data from users of your applications. If you use these features, you must comply with applicable law, including providing appropriate notices to users of your applications together with a copy of Microsoft's privacy statement. Our privacy statement is located at https://go.microsoft.com/fwlink/?LinkID=824704. You can learn more about data collection and use in the help documentation and our privacy statement. Your use of the software operates as your consent to these practices.

## Contributing ##

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.microsoft.com.

When you submit a pull request, a CLA-bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., label, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

For more details, please see [CONTRIBUTING.md](./CONTRIBUTING.md).
