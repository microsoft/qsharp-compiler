
Install:
https://github.com/microsoft/artifacts-credprovider


Run:
iex "& { $(irm https://aka.ms/install-artifacts-credprovider.ps1) }"

Run:
dotnet restore --interactive   

Env vars:
    QDK_TELEMETRY_OPT_OUT
    QDK_HOSTING_ENV
    ENABLE_QDK_TELEMETRY_EXCEPTIONS

Compiler directives
    DEBUG
    ENABLE_QDK_TELEMETRY_EXCEPTIONS



F#
install tool
https://github.com/fsprojects/fantomas
dotnet tool install -g fsharp

install extensions
Ionide-fsharp (there is a conflict with Shader Language support extension, as it will pick .fs files)
fantomas-fmt

dotnet fantomas --recurse .
