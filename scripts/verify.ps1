[CmdletBinding()]
param()
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
& (Join-Path $root 'build.ps1')
$testBin=Join-Path $root 'out\tests'
New-Item -ItemType Directory -Path $testBin -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $root 'DragonDisplay.exe') -Destination $testBin -Force
$compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$wpf=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\WPF'
foreach($testName in @('ContractTests','MotionAudit','Comparison')) {
    $arguments=@('/nologo','/target:exe','/platform:x64','/r:System.Web.Extensions.dll','/r:System.Xaml.dll',('/r:'+(Join-Path $wpf 'WindowsBase.dll')),('/r:'+(Join-Path $wpf 'PresentationCore.dll')),('/r:'+(Join-Path $wpf 'PresentationFramework.dll')),('/r:'+(Join-Path $root 'DragonDisplay.exe')),('/out:'+(Join-Path $testBin ($testName+'.exe'))),(Join-Path $root ('tests\'+$testName+'.cs')))
    & $compiler @arguments
    if($LASTEXITCODE -ne 0){throw "Failed to compile $testName"}
    & (Join-Path $testBin ($testName+'.exe')) $root
    if($LASTEXITCODE -ne 0){throw "$testName failed"}
}
[ordered]@{passed=$true;version=(Get-Content -LiteralPath (Join-Path $root 'VERSION') -Raw).Trim();checks=@('offline read-only contracts','six animated forms','light/dark comparison renders');scope='Offline tests only; no running Codex required';utc=[DateTime]::UtcNow.ToString('O')} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $root 'out\verification-summary.json') -Encoding UTF8
Write-Output 'PASS offline verification'
