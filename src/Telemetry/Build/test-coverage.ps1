# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Push-Location $PSScriptRoot/../

dotnet test --collect:"XPlat Code Coverage"

$coverageFile = (Get-ChildItem  $PSScriptRoot/../Tests/TestResults/*/coverage.cobertura.xml -Recurse |
                Sort-Object  -pro LastWriteTime -Descending |
                Select -First 1).FullName

Push-Location $PSScriptRoot/../Tests/
dotnet reportgenerator "-reports:$coverageFile" "-targetdir:TestResults/html" -reporttypes:HTML;
Pop-Location

$reportFile = Resolve-Path "$PSScriptRoot/../Tests/TestResults/html/index.htm"

Write-Output "Attempting to open: file:///$reportFile"

Start-Process "file:///$reportFile"

Pop-Location
