[CmdletBinding()]
param([string]$InstallDir='D:\Apps\CodexDragonCompanion',[string]$TaskName='CodexDragonCompanion-ReadOnly')
$ErrorActionPreference='Stop'
$exe=Join-Path ([IO.Path]::GetFullPath($InstallDir)) 'DragonDisplay.exe'
$task=Get-ScheduledTask -TaskName $TaskName -ErrorAction SilentlyContinue
if($task){if(@($task.Actions).Count -ne 1 -or [IO.Path]::GetFullPath($task.Actions[0].Execute) -ne $exe){throw 'Task points to another installation; refusing to change it'};Stop-ScheduledTask -TaskName $TaskName;Unregister-ScheduledTask -TaskName $TaskName -Confirm:$false}
Get-CimInstance Win32_Process -Filter "Name='DragonDisplay.exe'" | Where-Object ExecutablePath -eq $exe | ForEach-Object {Stop-Process -Id $_.ProcessId -Force}
Write-Output 'Companion task removed and matching process stopped. Files and runtime data are retained; Codex was not changed.'
