param(
    [string]
    $AssemblyVersion = $Env:ASSEMBLY_VERSION,

    [string]
    $SemverVersion = $Env:SEMVER_VERSION,

    [string]
    $NuGetVersion = $Env:NUGET_VERSION,

    [string]
    $VsixVersion = $Env:VSIX_VERSION
);

if ("$AssemblyVersion".Trim().Length -eq 0) {
    $Date = Get-Date;
    $Year = $Date.Year.ToString().Substring(2);
    $Month = $Date.Month.ToString().PadLeft(2, "0");
    $Hour   = (Get-Date).Hour.ToString().PadLeft(2, "0");
    $Minute   = (Get-Date).Minute.ToString().PadLeft(2, "0");
    $AssemblyVersion = "0.0.$Year$Month.$Hour$Minute";
}

if ("$SemverVersion".Trim().Length -eq 0) {
    $pieces = "$AssemblyVersion".split(".");
    $SemverVersion = "{0}.{1}.{2:d2}.{3.d2}" -f $pieces;
}

if ("$NuGetVersion".Trim().Length -eq 0) {
    $NuGetVersion = "$AssemblyVersion-alpha";
}

if ("$VsixVersion".Trim().Length -eq 0) {
    $VsixVersion = "$AssemblyVersion";
}

$Telemetry = "$($Env:ASSEMBLY_CONSTANTS)".Contains("TELEMETRY").ToString().ToLower();
Write-Output("Enable telemetry: $Telemetry");

Get-ChildItem -Recurse *.v.template `
    | ForEach-Object {
        $Source = $_.FullName;
        $Target = $Source.Substring(0, $Source.Length - 11);
        Write-Verbose "Replacing ASSEMBLY_VERSION with $AssemblyVersion in $Source and writing to $Target.";
        Get-Content $Source `
            | ForEach-Object {
                $_.
                    Replace("#ASSEMBLY_VERSION#", $AssemblyVersion).
                    Replace("#NUGET_VERSION#", $NuGetVersion).
                    Replace("#VSIX_VERSION#", $VsixVersion).
                    Replace("#SEMVER_VERSION#", $SemverVersion).
                    Replace("#ENABLE_TELEMETRY#", $Telemetry)
            } `
            | Set-Content $Target -NoNewline
    }

Push-Location (Join-Path $PSScriptRoot 'src/QsCompiler/Compiler')
.\FindNuspecReferences.ps1;
Pop-Location
