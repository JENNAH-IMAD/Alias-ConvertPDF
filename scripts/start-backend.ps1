param([switch]$Restart)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$apiBin = [IO.Path]::GetFullPath((Join-Path $root 'backend/BankStatementConverter.API/bin')) + [IO.Path]::DirectorySeparatorChar
if ($Restart) { & (Join-Path $PSScriptRoot 'stop-backend.ps1') }
$running = @(Get-Process -Name 'BankStatementConverter.API' -ErrorAction SilentlyContinue | Where-Object {
    $_.Path -and [IO.Path]::GetFullPath($_.Path).StartsWith($apiBin, [StringComparison]::OrdinalIgnoreCase)
})
if ($running.Count -gt 0) {
    throw "Le backend de ce projet est déjà actif (PID $($running.Id -join ', ')). Arrêtez-le avec Ctrl+C dans son terminal, ou utilisez scripts/start-backend.ps1 -Restart."
}
Set-Location $root
$config = Get-Content (Join-Path $root '.local/dev-secrets.json') -Raw | ConvertFrom-Json
$env:Jwt__Key = $config.jwt
$env:Seed__AdminEmail = $config.email
$env:Seed__AdminPassword = $config.password
$env:Database__AutoMigrate = 'true'
$env:ConnectionStrings__Default = $config.connectionString
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet run --project backend/BankStatementConverter.API --urls http://localhost:5080
if ($LASTEXITCODE -ne 0) { throw "Le backend s'est arrêté avec le code $LASTEXITCODE." }
