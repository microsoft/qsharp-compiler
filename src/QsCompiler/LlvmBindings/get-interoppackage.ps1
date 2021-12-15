if (Get-Command nuget -ErrorAction SilentlyContinue) {
    nuget install Microsoft.Quantum.LlvmBindings.Native -outputdirectory (Join-Path $PSScriptRoot drops) -version "13.0.0-CI-20220112-065039"
} else {
    Write-Host "##[error]Unable to get LlvmBindings.Native nuget package. Please install nuget.exe and/or mono."
    throw "Unable to get LlvmBindings.Native nuget package. Please install nuget.exe and/or mono."
}