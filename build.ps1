$ErrorActionPreference='Stop'
$root=$PSScriptRoot
$compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$wpf=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\WPF'
$compilerArgs=@('/nologo','/target:winexe','/optimize+','/platform:x64','/r:System.Web.Extensions.dll','/r:System.Xaml.dll',('/r:'+(Join-Path $wpf 'WindowsBase.dll')),('/r:'+(Join-Path $wpf 'PresentationCore.dll')),('/r:'+(Join-Path $wpf 'PresentationFramework.dll')),('/out:'+(Join-Path $root 'DragonDisplay.exe')))
$compilerArgs+=Get-ChildItem -LiteralPath (Join-Path $root 'src') -Filter '*.cs' | ForEach-Object FullName
& $compiler @compilerArgs
if($LASTEXITCODE -ne 0){throw 'Build failed'}
