# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

if ($IsWindows) {
    # find VS root
    $vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
    $visualStudioPath = & $vswhere -prerelease -latest -property installationPath
    Write-Output "vs located at: $visualStudioPath"

    # Call vcvars64.bat and write the set calls to file
    cmd.exe /c "call `"$visualStudioPath\VC\Auxiliary\Build\vcvars64.bat`" && set > %temp%\vcvars.txt"

    # Read the set calls and set the corresponding pwsh env vars
    Get-Content "$Env:temp\vcvars.txt" | Foreach-Object {
        if ($_ -match "^(.*?)=(.*)$") {
            Set-Content "env:\$($matches[1])" $matches[2]
            Write-Host "setting env: $($matches[1]) = $($matches[2])"
        }
    }
}
