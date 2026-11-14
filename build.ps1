# Publishes QuestDebloater.exe into .\dist
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$dist = Join-Path $root 'dist'
if (Test-Path $dist) { Remove-Item $dist -Recurse -Force }
New-Item -ItemType Directory -Force $dist | Out-Null

dotnet publish (Join-Path $root 'src\QuestDebloater\QuestDebloater.csproj') -c Release -r win-x64 -o $dist --nologo -v q
if ($LASTEXITCODE -ne 0) { throw 'dotnet publish failed' }

Copy-Item (Join-Path $root 'configs') (Join-Path $dist 'configs') -Recurse -Force
"Built $dist\QuestDebloater.exe ($((Get-Item (Join-Path $dist 'QuestDebloater.exe')).Length) bytes)"
