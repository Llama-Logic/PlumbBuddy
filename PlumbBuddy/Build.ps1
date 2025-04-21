$projectPath = ".\PlumbBuddy.csproj"
$outputDir = ".\AppxPackages\"

Write-Progress -Activity "Build for Windows" -Status "Getting Version" -PercentComplete 20
$csprojContent = Get-Content $projectPath -Raw
if ($csprojContent -match '<ApplicationVersion>(.*?)</ApplicationVersion>') {
    $version = $matches[1]
} else {
    Write-Error "Could not find ApplicationVersion in $projectPath"
    exit 1
}

Write-Progress -Activity "Build for Windows" -Status "Building $version x64" -PercentComplete 40
dotnet publish $projectPath /p:RuntimeIdentifier=win-x64 /p:AppxPackageDir=$outputDir
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}
Move-Item -Path ${outputDir}PlumbBuddy_${version}.0_Test\PlumbBuddy_${version}.0_x64.msix -Destination ${outputDir}PlumbBuddy_${version}_x64.msix -Force

Write-Progress -Activity "Build for Windows" -Status "Building $version arm64" -PercentComplete 60
dotnet publish $projectPath /p:RuntimeIdentifier=win-arm64 /p:AppxPackageDir=$outputDir
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}
Move-Item -Path ${outputDir}PlumbBuddy_${version}.0_Test\PlumbBuddy_${version}.0_arm64.msix -Destination ${outputDir}PlumbBuddy_${version}_arm64.msix -Force

Write-Progress -Activity "Build for Windows" -Status "Removing $version Workspace" -PercentComplete 80
Remove-Item -Path ${outputDir}PlumbBuddy_${version}.0_Test -Recurse

Write-Progress -Activity "Build for Windows" -Completed -Status "Built $version for x64 and arm64"