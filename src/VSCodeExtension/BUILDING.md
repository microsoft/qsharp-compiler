# Building the VS Code Extension Locally #

## Requirements ##

All Visual Studio Code extensions require a local install of the Node Package Manager to work.
NPM is distributed with Node.js, which can be obtained from https://nodejs.org/en/, or using Chocolatey:

```
choco install nodejs
```

Once you have NPM, we strongly suggest installing the VS Code Extension (vsce) tool:

```
npm i -g vsce
```

Here, `npm i -g` instructs NPM to *install globally* the vsce package.
After doing so, `vsce` can be used from the command line.

## Steps ##

Building the VS Code extension requires the following steps:

- Setting up the build pre-requisites (see below for details).
- Installing all Node.js dependencies required by the extension.
- Compiling the TypeScript for the extension itself.
  This step is automatically invoked when debugging the extension from within VS Code.

To set up the build pre-requisites, we require to run `bootstrap.ps1` from the root folder of the repository.
This in particular creates `package.json` from `package.json.v.template`, which specifies all of the dependencies that must be installed in order for the TypeScript comprising the VS Code extension to successfully compile and run.
All following steps need to be executed from within the root directory of the extension (this directory).

Once `package.json` exists, run
```
npm i
```
from within this directory to install the Node.js dependencies.

Finally, to produce a VSIX that can be used to locally install, rather than debug, the extension, run:

```
vsce package
```

## Debugging ##

As an alternative to using `vsce package` to produce an installable VSIX, the VS Code extension can be run in an experimental instance of VS Code.
To do so, make sure that you have run the build procedure above through to calling `Build-Dependencies.ps1`.
Then, from the Debug tab in VS Code, ensure that "Extension" is selected in the Debug target menu and press the green ‣.

If you would like to also debug the language server at the same time, this can be done using the ".NET Core Attach" debugging target along with the "Extension" target.
Set a breakpoint in `extension.ts` that will let you intercept the language server executable before any calls are made from the client.
For instance, breaking at the second line below will let you observe the PID used to attach the .NET Core debugger and will prevent any calls from the client to the server until you resume the extension:

```typescript
.then((childProcess) => {
    console.log(`[qsharp-lsp] started QsLanguageServer.exe as PID ${childProcess.pid}.`);
})
```

Once you reach this breakpoint, start a ".NET Core Attach" debugging session and select the PID that you observe in the above snippet.
Change back to the "Extension" debugger and resume.

## Common Problems ##

### Semver Issues ###

You may see an error when packaging or debugging the VS Code extension indicating that the package is not semver compatible.
This indicates that the build script `bootstrap.ps1` did not produce a SemVer 2.0–compatible version number when writing `package.json` from `package.json.v.template`, typically due to a mis-set environment variable.
If this happens, correct the environment variables used by `bootstrap.ps1` or manually edit the `version` property of `package.json` (note that any such manual edits **will** be overwritten by calls to `bootstrap.ps1` and will not be saved in the repo).