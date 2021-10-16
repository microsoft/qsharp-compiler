dotnet test --collect:"XPlat Code Coverage"

$coverageFile = (Get-ChildItem  $PSScriptRoot/../Tests/TestResults/*/coverage.cobertura.xml -Recurse |
                Sort-Object  -pro LastWriteTime -Descending |
                Select -First 1).FullName

Push-Location $PSScriptRoot/../Tests/
dotnet reportgenerator "-reports:$coverageFile" "-targetdir:TestResults/html" -reporttypes:HTML;
Pop-Location

Start-Process "file:///$PSScriptRoot/../Tests/TestResults/html/index.htm"
