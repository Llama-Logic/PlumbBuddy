param (
    [string]$ProjectDir
)

$proxyPath = Join-Path $ProjectDir "Proxy"
Get-ChildItem $proxyPath -Recurse -Filter *.pyc | Remove-Item -Force
