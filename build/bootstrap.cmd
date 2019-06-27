:: Initializes the current repo
:: and prepares it for build
@echo off

dotnet  --info || GOTO missingDotnet
git --version  || GOTO missingGit

:: Set Build number on all files that uses it
call powershell -NoProfile build\setVersionNumber.ps1

:: Initialize the compiler's nuspec file
CALL :nuspecBootstrap

:: Done
GOTO EOF

:: Bootstrap the compiler nuspec
:nuspecBootstrap
pushd src\QsCompiler\Compiler
call powershell -NoProfile .\FindNuspecReferences.ps1
popd
EXIT /B

:missingGit
echo.
echo This script depends on git.
echo.
echo Make sure you install it as part of Visual Studio, and then run this
echo script inside the "Developer Command Prompt for VS 2017"
echo.
EXIT /B 1001

:missingDotnet
echo.
echo You need to install dotnet core to use/build Solid:
echo https://www.microsoft.com/net/download
echo.
EXIT /B 1002

:EOF
