param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$project = Join-Path $PSScriptRoot "SelfishNetV3\SelfishNet.csproj"

dotnet publish $project -c $Configuration -p:PublishProfile=win-x86-framework-dependent
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

dotnet publish $project -c $Configuration -p:PublishProfile=win-x86-self-contained
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host ""
Write-Host "Builds created:"
Write-Host "- SelfishNetV3\publish\win-x86-framework-dependent  (requires .NET Desktop Runtime)"
Write-Host "- SelfishNetV3\publish\win-x86-self-contained      (does not require .NET install)"
Write-Host ""
Write-Host "Both builds still require WinPcap/Npcap for packet capture features."
