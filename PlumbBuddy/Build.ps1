$projectPath = ".\PlumbBuddy.csproj"
$outputDir = ".\AppxPackages\"

$csprojContent = Get-Content $projectPath -Raw
if ($csprojContent -match '<ApplicationVersion>(.*?)</ApplicationVersion>') {
    $version = $matches[1]
} else {
    Write-Error "Could not find ApplicationVersion in $projectPath"
    exit 1
}

dotnet publish $projectPath /p:RuntimeIdentifier=win-x64 /p:AppxPackageDir=$outputDir
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}
Move-Item -Path ${outputDir}PlumbBuddy_${version}.0_Test\PlumbBuddy_${version}.0_x64.msix -Destination ${outputDir}PlumbBuddy_${version}_x64.msix -Force

dotnet publish $projectPath /p:RuntimeIdentifier=win-arm64 /p:AppxPackageDir=$outputDir
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}
Move-Item -Path ${outputDir}PlumbBuddy_${version}.0_Test\PlumbBuddy_${version}.0_arm64.msix -Destination ${outputDir}PlumbBuddy_${version}_arm64.msix -Force

Remove-Item -Path ${outputDir}PlumbBuddy_${version}.0_Test -Recurse