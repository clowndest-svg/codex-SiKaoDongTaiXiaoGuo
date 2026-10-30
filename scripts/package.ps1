[CmdletBinding()]
param([switch]$SkipBuild)
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
if(!$SkipBuild){& (Join-Path $root 'build.ps1')}
if(!(Test-Path -LiteralPath (Join-Path $root 'DragonDisplay.exe'))){throw 'Build executable is missing'}
$version=(Get-Content -LiteralPath (Join-Path $root 'VERSION') -Raw).Trim()
$packageDir=Join-Path $root 'out\packages'
New-Item -ItemType Directory -Path $packageDir -Force | Out-Null
$names=@('src','assets','scripts','tests','docs','README.md','VERSION','build.ps1','启动龙娘.cmd','停止龙娘.ps1','形态对比说明.md','DragonDisplay.exe')
$files=@($names | ForEach-Object {Join-Path $root $_})
$zip=Join-Path $packageDir ('DragonCompanion-'+$version+'-windows-x64.zip')
Compress-Archive -LiteralPath $files -DestinationPath $zip -Force
Get-FileHash -LiteralPath $zip -Algorithm SHA256 | Select-Object Hash,Path
