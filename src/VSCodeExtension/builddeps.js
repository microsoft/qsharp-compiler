#!/usr/bin/env node
// This file is needed to detect whether PowerShell is provided
// as `powershell` (Windows PowerShell, version 5.1 and earlier),
// or as `pwsh` (PowerShell Core, version 6, or PowerShell, version
// 7 and later).
// That detection is a little more complicated than can be done with
// the intersection of /bin/sh and cmd.exe that is supported by the
// scripts key of the package.json spec, so we resolve that with a small
// node file instead.
// Note that this file does not use TypeScript, as the tsc command is not
// yet available at this point.

// IMPORTS ///////////////////////////////////////////////////////////////////

const which = require('which');
const { spawn } = require('child_process');

// MAIN //////////////////////////////////////////////////////////////////////

let powershell = which.sync('pwsh', {nothrow: true});
if (powershell == null) {
    powershell = which.sync('powershell');
}
console.log("Using PowerShell executable", powershell);

spawn(powershell, ["-NoProfile", "-File", "Build-Dependencies.ps1"], {
    stdio: ['inherit', 'inherit', 'inherit']
})
