$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
Set-Location $root
$config = Get-Content (Join-Path $root '.local/dev-secrets.json') -Raw | ConvertFrom-Json
$env:Jwt__Key = $config.jwt
$env:Seed__AdminEmail = $config.email
$env:Seed__AdminPassword = $config.password
$env:Database__AutoMigrate = 'true'
$env:ConnectionStrings__Default = $config.connectionString
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet run --project backend/BankStatementConverter.API --urls http://localhost:5080
