param (
    [string]$ProjectDir
)

$proxyPath = Join-Path $ProjectDir "Proxy"
$includePatterns = @(
    '*.pyc', '*.html', '*.md', '*.css', '*.scss', '*.js', '*.json', '*.mjs',
    '*.ts', '*.vue', '*.jpg', '*.jpeg', '*.gif', '*.png', '*.ico', '*.ttf',
    '*.otf', '*.eot', '*.woff', '*.woff2', '*.browserslistrc', '*.editorconfig'
)
$tempDir = Join-Path $env:TEMP ("proxybuild_" + [guid]::NewGuid())
Copy-Item $proxyPath $tempDir -Recurse -Force -Exclude node_modules
Get-ChildItem $tempDir -Recurse -Filter *.py | Remove-Item -Force
Get-ChildItem -Path $tempDir -Recurse | Where-Object { $_.FullName -match '\\node_modules\\' } | Remove-Item -Force
$scriptArchivePath = Join-Path $ProjectDir "PlumbBuddy_Proxy.zip"
Compress-Archive -Path $tempDir\* -DestinationPath $scriptArchivePath -Force
Remove-Item -Path $tempDir -Recurse -Force
$scriptModPath = Join-Path $ProjectDir "PlumbBuddy_Proxy.ts4script"
if (Test-Path $scriptModPath -PathType Leaf) {
    Remove-Item -Path $scriptModPath -Force
}
Rename-Item -Path $scriptArchivePath -NewName $scriptModPath
