$target=Join-Path $PSScriptRoot 'DragonDisplay.exe'
Get-Process -Name DragonDisplay -ErrorAction SilentlyContinue | Where-Object { $_.Path -eq $target } | Stop-Process
