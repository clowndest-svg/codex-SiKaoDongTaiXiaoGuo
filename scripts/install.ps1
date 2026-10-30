[CmdletBinding()]
param([string]$InstallDir='D:\Apps\CodexDragonCompanion',[string]$TaskName='CodexDragonCompanion-ReadOnly',[switch]$NoStart)
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$target=[IO.Path]::GetFullPath($InstallDir).TrimEnd('\')
$source=[IO.Path]::GetFullPath($root).TrimEnd('\')
if($target -eq [IO.Path]::GetPathRoot($target).TrimEnd('\') -or $target -eq $source -or $source.StartsWith($target+'\',[StringComparison]::OrdinalIgnoreCase) -or $target.StartsWith($source+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Use a dedicated installation directory outside this checkout'}
$exe=Join-Path $target 'DragonDisplay.exe'
$task=Get-ScheduledTask -TaskName $TaskName -ErrorAction SilentlyContinue
if($task -and (@($task.Actions).Count -ne 1 -or [IO.Path]::GetFullPath($task.Actions[0].Execute) -ne $exe)){throw 'Task name belongs to a different installation; use its directory or a different -TaskName'}
if((Test-Path -LiteralPath $target) -and @(Get-ChildItem -LiteralPath $target -Force).Count -gt 0 -and !(Test-Path -LiteralPath (Join-Path $target '.dragon-companion-install.json')) -and !((Test-Path -LiteralPath $exe) -and (Test-Path -LiteralPath (Join-Path $target 'src\App.cs')))){throw 'Target contains unrelated files; choose an empty directory'}
& (Join-Path $root 'build.ps1')
$names=@('src','assets','scripts','tests','docs','README.md','VERSION','build.ps1','启动龙娘.cmd','停止龙娘.ps1','形态对比说明.md','DragonDisplay.exe')
New-Item -ItemType Directory -Path $target -Force | Out-Null
$backup=Join-Path $target ('backups\before-install-'+[Guid]::NewGuid().ToString('N'))
$oldNames=@($names | Where-Object {Test-Path -LiteralPath (Join-Path $target $_)})
if($oldNames.Count -gt 0){New-Item -ItemType Directory -Path $backup -Force | Out-Null;foreach($name in $oldNames){Copy-Item -LiteralPath (Join-Path $target $name) -Destination $backup -Recurse -Force}}
if($task){Stop-ScheduledTask -TaskName $TaskName}
Get-CimInstance Win32_Process -Filter "Name='DragonDisplay.exe'" | Where-Object ExecutablePath -eq $exe | ForEach-Object {Stop-Process -Id $_.ProcessId -Force;Wait-Process -Id $_.ProcessId -Timeout 8 -ErrorAction SilentlyContinue}
Start-Sleep -Milliseconds 800
try {
    foreach($name in $names){Copy-Item -LiteralPath (Join-Path $root $name) -Destination $target -Recurse -Force}
    foreach($file in Get-ChildItem -LiteralPath (Join-Path $root 'assets') -File){if((Get-FileHash -LiteralPath $file.FullName).Hash -ne (Get-FileHash -LiteralPath (Join-Path $target ('assets\'+$file.Name))).Hash){throw 'Asset copy verification failed'}}
    if((Get-FileHash -LiteralPath (Join-Path $root 'DragonDisplay.exe')).Hash -ne (Get-FileHash -LiteralPath $exe).Hash){throw 'Executable copy verification failed'}
    $who=[Security.Principal.WindowsIdentity]::GetCurrent().Name
    $action=New-ScheduledTaskAction -Execute $exe -WorkingDirectory $target
    $trigger=New-ScheduledTaskTrigger -AtLogOn -User $who
    $principal=New-ScheduledTaskPrincipal -UserId $who -LogonType Interactive -RunLevel Limited
    $settings=New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -ExecutionTimeLimit ([TimeSpan]::Zero) -MultipleInstances IgnoreNew -RestartCount 3 -RestartInterval (New-TimeSpan -Minutes 1)
    Register-ScheduledTask -TaskName $TaskName -Action $action -Trigger $trigger -Principal $principal -Settings $settings -Description 'Read-only dragon companion; does not modify Codex or its updater.' -Force | Out-Null
    @{version=(Get-Content -LiteralPath (Join-Path $root 'VERSION') -Raw).Trim();taskName=$TaskName} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $target '.dragon-companion-install.json') -Encoding UTF8
    if(!$NoStart){Start-ScheduledTask -TaskName $TaskName}
} catch {
    foreach($name in $oldNames){Copy-Item -LiteralPath (Join-Path $backup $name) -Destination $target -Recurse -Force}
    if($task -and !$NoStart){Start-ScheduledTask -TaskName $TaskName -ErrorAction SilentlyContinue}
    throw
}
Write-Output "Installed to $target. Task: $TaskName. Runtime settings preserved."
