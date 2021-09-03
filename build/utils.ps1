# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

if(Test-Path function:\exec) {
    # already included
    return
}

function Test-BuildCompiler {
    if (!(Test-Path env:\ENABLE_COMPILER)) {
        $true
    }
    else {
        # Coerce falsy values
        # negate to keep the defalt $true
        $env:ENABLE_COMPILER -ne $false
    }
}

function Test-BuildVsix {
    ($Env:ENABLE_VSIX -ne "false") -and $IsWindows
}

function Test-BuildLlvmComponents {
    ((Test-Path env:\ENABLE_LLVM_BUILDS) -and ($env:ENABLE_LLVM_BUILDS -eq $true))
}

# Utilities
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

function Write-Vso {
    param (
        [Parameter(Mandatory = $true)]
        [string]$message,
        [Parameter(Mandatory = $false)]
        [ValidateSet("group", "warning", "error", "section", "debug", "command", "endgroup")]
        [string]$severity = "debug"
    )
    Write-Host "##[$severity]$message"
}

function Test-CommandExists($name) {
    $null -ne (Get-Command $name -ErrorAction SilentlyContinue)
  }
