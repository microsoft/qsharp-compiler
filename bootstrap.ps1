[CmdletBinding()]
param(
    [string]
    $AssemblyVersion = $Env:ASSEMBLY_VERSION,

    [string]
    $SemverVersion = $Env:SEMVER_VERSION,

    [string]
    $NuGetVersion = $Env:NUGET_VERSION,

    [string]
    $VsVsixVersion = $Env:VSVSIX_VERSION
);

if ("$AssemblyVersion".Trim().Length -eq 0) {
    $Date = Get-Date;
    $Year = $Date.Year.ToString().Substring(2);
    $Month = $Date.Month.ToString().PadLeft(2, "0");
    $Hour   = (Get-Date).Hour.ToString().PadLeft(2, "0");
    $Minute   = (Get-Date).Minute.ToString().PadLeft(2, "0");
    $AssemblyVersion = "0.9999.$Year$Month.$Hour$Minute";
}

Write-Host "Assembly version: $AssemblyVersion";
$pieces = "$AssemblyVersion".split(".");
$MajorVersion = "$($pieces[0])";
$MinorVersion = "$($pieces[1])";
$patch = "$($pieces[2])"
$rev = "$($pieces[3])".PadLeft(4, "0");

if ("$SemverVersion".Trim().Length -eq 0) {
    $SemverVersion = "$MajorVersion.$MinorVersion.$patch$rev";
}

if ("$NuGetVersion".Trim().Length -eq 0) {
    $NuGetVersion = "$AssemblyVersion-alpha";
}

if ("$VsVsixVersion".Trim().Length -eq 0) {
    $VsVsixVersion = "$MajorVersion.$MinorVersion.$patch.$rev";
}

$Telemetry = "$($Env:ASSEMBLY_CONSTANTS)".Contains("TELEMETRY").ToString().ToLower();
Write-Host "Enable telemetry: $Telemetry";

Get-ChildItem -Recurse *.v.template `
    | ForEach-Object {
        $Source = $_.FullName;
        $Target = $Source.Substring(0, $Source.Length - 11);
        Write-Verbose "Replacing ASSEMBLY_VERSION with $AssemblyVersion in $Source and writing to $Target.";
        Get-Content $Source `
            | ForEach-Object {
                $_.
                    Replace("#MAJOR_VERSION#", $MajorVersion).
                    Replace("#MINOR_VERSION#", $MinorVersion).
                    Replace("#ASSEMBLY_VERSION#", $AssemblyVersion).
                    Replace("#NUGET_VERSION#", $NuGetVersion).
                    Replace("#VSVSIX_VERSION#", $VsVsixVersion).
                    Replace("#SEMVER_VERSION#", $SemverVersion).
                    Replace("#ENABLE_TELEMETRY#", $Telemetry)
            } `
            | Set-Content $Target 
    }

If ($Env:ASSEMBLY_VERSION -eq $null) { $Env:ASSEMBLY_VERSION ="$AssemblyVersion" }
If ($Env:NUGET_VERSION -eq $null) { $Env:NUGET_VERSION ="$NuGetVersion" }
If ($Env:SEMVER_VERSION -eq $null) { $Env:SEMVER_VERSION ="$SemverVersion" }
If ($Env:VSVSIX_VERSION -eq $null) { $Env:VSVSIX_VERSION ="$VsVsixVersion" }
Write-Host "##vso[task.setvariable variable=VsVsix.Version]$VsVsixVersion"

Write-Host "##[info]Finding NuSpec references..."
Push-Location (Join-Path $PSScriptRoot 'src/QsCompiler/Compiler')
.\FindNuspecReferences.ps1;
Pop-Location
