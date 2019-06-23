::
:: This script finds all *.v.template files and replaces
:: their build tokens (e.g: #NUGET_VERSION#) with a valid value
:: based on the MAJOR.MINOR.BUILDNUMBER
::
SET DATE=%date:~12,2%%date:~4,2%
SET TIME=%time:~0,2%%time:~3,2%
IF "%BUILD_MAJOR%" == "" SET BUILD_MAJOR=0
IF "%BUILD_MINOR%" == "" SET BUILD_MINOR=0
IF "%BUILD_BUILDNUMBER%" == "" SET BUILD_BUILDNUMBER=%BUILD_MAJOR%.%BUILD_MINOR%.%DATE%.%TIME%

SET REVISION=%BUILD_BUILDNUMBER:~-9%
IF "%REVISION:~0,1%" == "." SET REVISION=%REVISION:~1%

IF "%ASSEMBLY_VERSION%" == "" SET ASSEMBLY_VERSION=%BUILD_MAJOR%.%BUILD_MINOR%.%REVISION%
IF "%VSIX_VERSION%" == "" SET VSIX_VERSION=%ASSEMBLY_VERSION%
IF "%PYTHON_VERSION%" == "" SET PYTHON_VERSION=%ASSEMBLY_VERSION%a1
IF "%SEMVER_VERSION%" == "" SET SEMVER_VERSION=%BUILD_MAJOR%.%BUILD_MINOR%.%REVISION:.=%
IF "%NUGET_VERSION%" == "" SET NUGET_VERSION=%VSIX_VERSION%-alpha

FOR /R %%F IN (*.v.template) DO CALL :updateOne %%F
GOTO :done

:updateOne
set original=%1
SET target=%original:~0,-11%
powershell -NoProfile -Command "(Get-Content %original% -Raw) |  ForEach-Object { $_.replace('#ASSEMBLY_VERSION#', '%ASSEMBLY_VERSION%').replace('#NUGET_VERSION#', '%NUGET_VERSION%').replace('#VSIX_VERSION#', '%VSIX_VERSION%').replace('#SEMVER_VERSION#', '%SEMVER_VERSION%') } | Set-Content %target% -NoNewline"

:done
