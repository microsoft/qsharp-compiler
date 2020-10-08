# Building the VS Code Extension Locally #

## Prerequisites ##

- NPM
- PowerShell Core (6.0 or later)
- .NET Core SDK 3.1 or later

### Obtaining NPM ###

All Visual Studio Code extensions require a local install of the Node Package Manager to work.
NPM is distributed with Node.js, which can be obtained from https://nodejs.org/en/, or using Chocolatey:

```
choco install nodejs
```

## Steps ##

Before doing any local development, you will need to run `bootstrap.ps1` to set version numbers and other metadata for the Quantum Development Kit.
Please see [the root README](../../README.md) for more information.

You can then install the dependencies needed by the Visual Studio Code extension by running this command in the `VSCodeExtension` folder:

```
npm install
```

To package the extension into a VSIX file that can be installed from Visual Studio Code, run:

```
npx vsce package
```

As of version 0.10 of the Quantum Development Kit, the Visual Studio Code extension no longer includes the Q# language server, but downloads a copy of the server at runtime.
This process is controlled by metadata stored in `package.json`, which is in turn generated from `package.json.v.template` when running `bootstrap.ps1` in the root of the repository.
For instance, when developing against 0.10.1910.3002, `bootstrap.ps1` will create a `blobs` section in `package.json` similar to the following:

```json
"blobs": {
    "win32": {
        "url": "https://msquantumpublic.blob.core.windows.net/qsharp-compiler/QsLanguageServer-win10-x64-0.0.1910.1532.zip",
        "sha256": "<DISABLED>"
    },
    "darwin": {
        "url": "https://msquantumpublic.blob.core.windows.net/qsharp-compiler/QsLanguageServer-osx-x64-0.0.1910.1532.zip",
        "sha256": "<DISABLED>"
    },
    "linux": {
        "url": "https://msquantumpublic.blob.core.windows.net/qsharp-compiler/QsLanguageServer-linux-x64-0.0.1910.1532.zip",
        "sha256": "<DISABLED>"
    }
}
```

Here, the `<DISABLED>` value for hashes instructs the Visual Studio Code extension to suppress checking SHA256 sums of downloaded blobs, as these hashes are injected by the Quantum Development Kit build process at release time.
If no released version of the Q# language server has been published matching the version number injected by `bootstrap.ps1`, then downloading the language server will fail when running the extension.

For local development of the extension, however, it is typically more convenient to suppress downloading the Q# language server at all, and to use a local development version of the server instead.
If the `quantumDevKit.languageServerPath` preference is set either in your Visual Studio Code user preferences or in your folder or workspace settings, then the executable at that path will be used in preference to downloading a copy of the Q# language server, and no attempt will be made to ensure that the executable at that path matches the extension version.

You can build a self-contained version of the language server for use in local development using the `dotnet publish` command (replace your `--runtime` argument as needed):

```pwsh
PS> cd ../QsCompiler/LanguageServer
PS> dotnet publish --self-contained --runtime win10-x64
```

To get the value that you need for the `quantumDevKit.languageServerPath` preference:

```
PS> Resolve-Path bin/Debug/netcoreapp3.1/win10-x64/publish/Microsoft.Quantum.QsLanguageServer.exe
```

## Debugging ##

As an alternative to using `npx vsce package` to produce an installable VSIX, the VS Code extension can be run in an experimental instance of VS Code.
From the Debug tab in VS Code, ensure that "Extension" is selected in the Debug target menu and press the green ‣.

If you would like to also debug the language server at the same time, this can be done using the ".NET Core Attach" debugging target along with the "Extension" target.
Set a breakpoint in `languageServer.ts` that will let you intercept the language server executable before any calls are made from the client.
For instance, breaking at the second line below will let you observe the PID used to attach the .NET Core debugger and will prevent any calls from the client to the server until you resume the extension:

```typescript
.then((childProcess) => {
    console.log(`[qsharp-lsp] started Q# Language Server as PID ${childProcess.pid}.`);
})
```

Once you reach this breakpoint, start a ".NET Core Attach" debugging session and select the PID that you observe in the above snippet.
Change back to the "Extension" debugger and resume.

## Common Problems ##

### Semver Issues ###

You may see an error when packaging or debugging the VS Code extension indicating that the package is not semver compatible.
This indicates that the build script `bootstrap.ps1` did not produce a SemVer 2.0–compatible version number when writing `package.json` from `package.json.v.template`, typically due to a mis-set environment variable.
If this happens, correct the environment variables used by `bootstrap.ps1` or manually edit the `version` property of `package.json` (note that any such manual edits **will** be overwritten by calls to `bootstrap.ps1` and will not be saved in the repo).
