# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

if (Test-Path function:\exec) {
    # already included
    return
}

# Fix temp path for non-windows platforms if missing
if (!(Test-Path env:\TEMP)) {
    $env:TEMP = [System.IO.Path]::GetTempPath()
}

####
# Utilities
####

# executes the sciptblock. If the working directory is specitied, the block is
# exeucted in that directory. If the $LASTEXITCODE from the block is not 0,
# an exception is thrown.
function exec {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [scriptblock]$cmd,
        [string]$errorMessage = "Failed to execute command",
        [Alias("wd")]
        [string]$workingDirectory = $null
    )
    try {

        if ($workingDirectory) {
            Push-Location -Path $workingDirectory
        }

        $global:lastexitcode = 0
        & $cmd
        if ($global:lastexitcode -ne 0) {
            throw "exec: $errorMessage"
        }
    }
    finally {
        if ($workingDirectory) {
            Pop-Location
        }
    }
}

# tests whether a condition is true. Throws an exception with the specified error message
# if the condition check fails.
function Assert {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        $conditionToCheck,

        [Parameter(Mandatory = $true)]
        [string]$failureMessage
    )

    if (-not $conditionToCheck) {
        throw ('Assert: {0}' -f $failureMessage)
    }
}

# Writes an Azure DevOps message with default debug severity
function Write-AdoLog {
    param (
        [Parameter(Mandatory = $true)]
        [string]$message,
        [Parameter(Mandatory = $false)]
        [ValidateSet("group", "warning", "error", "section", "debug", "command", "endgroup")]
        [string]$severity = "debug"
    )
    Write-Host "##[$severity]$message"
}

# Returns true if a command with the specified name exists.
function Test-CommandExists($name) {
    $null -ne (Get-Command $name -ErrorAction SilentlyContinue)
}

# Gets the LLVM version git hash
# on the CI this will come as an env var
function Get-LlvmTag {
    $sha = exec { Get-Content (Join-Path $PSScriptRoot llvm.tag.txt) }
    Write-AdoLog "Use cached submodule version: $sha"
    $sha
}
