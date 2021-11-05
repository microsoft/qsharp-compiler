
Install:
https://github.com/microsoft/artifacts-credprovider


Run:
iex "& { $(irm https://aka.ms/install-artifacts-credprovider.ps1) }"

Run:
dotnet restore --interactive

dotnet nuget push ./microsoft.applications.events.client.1.1.1.337.nupkg --source https://ms-quantum.pkgs.visualstudio.com/_packaging/alpha/nuget/v3/index.json --interactive --api-key AzureDevOps


Env vars:
    QDK_TELEMETRY_OPT_OUT
    QDK_HOSTING_ENV
    ENABLE_QDK_TELEMETRY_EXCEPTIONS

Compiler directives
    DEBUG
    ENABLE_QDK_TELEMETRY_EXCEPTIONS



F#
install tool Fantomas
    https://github.com/fsprojects/fantomas
    dotnet tool install -g fantomas-tool

install extensions
    PowerShell
    C#
    Ionide-fsharp (there is a conflict with Shader Language support extension, as it will pick .fs files)
    fantomas-fmt

dotnet tool restore --configfile .\Nuget.config

dotnet fantomas --recurse .
