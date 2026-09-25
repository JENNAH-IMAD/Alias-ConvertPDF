$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$apiBin = [IO.Path]::GetFullPath((Join-Path $root 'backend/BankStatementConverter.API/bin')) + [IO.Path]::DirectorySeparatorChar

# Match the executable inside this checkout, never every dotnet process or a stale PID.
$instances = @(Get-Process -Name 'BankStatementConverter.API' -ErrorAction SilentlyContinue | Where-Object {
    $_.Path -and [IO.Path]::GetFullPath($_.Path).StartsWith($apiBin, [StringComparison]::OrdinalIgnoreCase)
})
foreach ($instance in $instances) {
    Stop-Process -InputObject $instance -ErrorAction Stop
    $instance.WaitForExit()
    Write-Output "Backend local arrêté (PID $($instance.Id))."
}
if ($instances.Count -eq 0) { Write-Output 'Aucun backend actif dans ce projet.' }
