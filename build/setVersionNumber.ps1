param(
    [string]
    $AssemblyVersion = $Env:ASSEMBLY_VERSION,

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

if ("$NuGetVersion".Trim().Length -eq 0) {
    $NuGetVersion = "$AssemblyVersion-alpha";
}

if ("$VsixVersion".Trim().Length -eq 0) {
    $VsixVersion = "$AssemblyVersion";
}

Get-ChildItem -Recurse *.v.template `
    | ForEach-Object {
        $Source = $_.FullName;
        $Target = $Source.Substring(0, $Source.Length - 11);
        Write-Verbose "Replacing ASSEMBLY_VERSION with $AssemblyVersion in $Source and writing to $Target.";
        Get-Content $Source `
            | ForEach-Object {
                $_.
                    Replace("#ASSEMBLY_VERSION#", $AssemblyVersion).
                    Replace("#NUGET_VERSION#", $NuGetVersion)
                    Replace("#VSIX_VERSION#", $VsixVersion)
            } `
            | Set-Content $Target -NoNewline
    }