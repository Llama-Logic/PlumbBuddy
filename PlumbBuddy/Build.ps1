$projectPath = ".\PlumbBuddy.csproj"
$terminalProjectDirectory = "..\PlumbBuddy.Terminal"
$terminalProjectPath = "$terminalProjectDirectory\PlumbBuddy.Terminal.csproj"
$outputDir = ".\AppxPackages\"

Write-Progress -Activity "Build for Windows" -Status "Getting Version" -PercentComplete 10
$csprojContent = Get-Content $projectPath -Raw
if ($csprojContent -match '<ApplicationVersion>(.*?)</ApplicationVersion>') {
    $version = $matches[1]
} else {
    Write-Error "Could not find ApplicationVersion in $projectPath"
    exit 1
}

Write-Progress -Activity "Build for Windows" -Status "Building $version Windows CLI x64" -PercentComplete 20
dotnet publish $terminalProjectPath -c Release /p:RuntimeIdentifier=win-x64
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}
Compress-Archive -Path $terminalProjectDirectory\bin\Release\net8.0\win-x64\publish\pb.exe -DestinationPath ${outputDir}\PlumbBuddyCLI_${version}_win-x64.zip

Write-Progress -Activity "Build for Windows" -Status "Building $version Windows CLI arm64" -PercentComplete 25
dotnet publish $terminalProjectPath -c Release /p:RuntimeIdentifier=win-arm64
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}
Compress-Archive -Path $terminalProjectDirectory\bin\Release\net8.0\win-arm64\publish\pb.exe -DestinationPath ${outputDir}\PlumbBuddyCLI_${version}_win-arm64.zip

Write-Progress -Activity "Build for Windows" -Status "Building $version macOS CLI x64" -PercentComplete 30
dotnet publish $terminalProjectPath -c Release /p:RuntimeIdentifier=osx-x64
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}
Compress-Archive -Path $terminalProjectDirectory\bin\Release\net8.0\osx-x64\publish\pb -DestinationPath ${outputDir}\PlumbBuddyCLI_${version}_osx-x64.zip

Write-Progress -Activity "Build for Windows" -Status "Building $version macOS CLI arm64" -PercentComplete 35
dotnet publish $terminalProjectPath -c Release /p:RuntimeIdentifier=osx-arm64
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}
Compress-Archive -Path $terminalProjectDirectory\bin\Release\net8.0\osx-arm64\publish\pb -DestinationPath ${outputDir}\PlumbBuddyCLI_${version}_osx-arm64.zip

Write-Progress -Activity "Build for Windows" -Status "Building $version Linux CLI x64" -PercentComplete 40
dotnet publish $terminalProjectPath -c Release /p:RuntimeIdentifier=linux-x64
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}
Compress-Archive -Path $terminalProjectDirectory\bin\Release\net8.0\linux-x64\publish\pb -DestinationPath ${outputDir}\PlumbBuddyCLI_${version}_linux-x64.zip

Write-Progress -Activity "Build for Windows" -Status "Building $version Linux CLI arm64" -PercentComplete 45
dotnet publish $terminalProjectPath -c Release /p:RuntimeIdentifier=linux-arm64
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}
Compress-Archive -Path $terminalProjectDirectory\bin\Release\net8.0\linux-arm64\publish\pb -DestinationPath ${outputDir}\PlumbBuddyCLI_${version}_linux-arm64.zip

Write-Progress -Activity "Build for Windows" -Status "Building $version x64" -PercentComplete 50
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

Write-Progress -Activity "Build for Windows" -Status "Timestamping x64" -PercentComplete 87
signtool sign /fd SHA256 /tr http://timestamp.sectigo.com /td SHA256 /sha1 0c3a6be3d44b381a2185f90423fcc567a6cb4338 "${outputDir}PlumbBuddy_${version}_x64.msix"

Write-Progress -Activity "Build for Windows" -Status "Timestamping amd64" -PercentComplete 94
signtool sign /fd SHA256 /tr http://timestamp.sectigo.com /td SHA256 /sha1 0c3a6be3d44b381a2185f90423fcc567a6cb4338 "${outputDir}PlumbBuddy_${version}_arm64.msix"

Write-Progress -Activity "Build for Windows" -Completed -Status "Built $version for x64 and arm64"