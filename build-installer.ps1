param(
    [string]$Version = "",
    [string]$Iscc = "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe"
)
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $root
if ($Version -eq "") {
    [xml]$proj = Get-Content "$root\ScrollShot.csproj"
    $Version = ($proj.Project.PropertyGroup | Where-Object { $_.Version } | Select-Object -First 1).Version
}
$publish = "$root\publish"
if (Test-Path $publish) { Remove-Item $publish -Recurse -Force }
dotnet publish "$root\ScrollShot.csproj" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:Version=$Version -o $publish
if ($LASTEXITCODE -ne 0) { throw "publish failed" }
if (-not (Test-Path $Iscc)) {
    $alt = "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe"
    if (Test-Path $alt) { $Iscc = $alt } else { Write-Host "Inno Setup not found. Published files are in $publish"; exit 0 }
}
& $Iscc "/DAppVersion=$Version" "/DSourceDir=$publish" "$root\installer\ScrollShot.iss"
if ($LASTEXITCODE -ne 0) { throw "installer failed" }
Write-Host "Installer: $root\installer-output\ScrollShot-Setup-$Version.exe"
