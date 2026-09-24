param([ValidateSet('Before','After')][string]$Phase = 'Before')
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$config = Get-Content (Join-Path $root '.local/dev-secrets.json') -Raw | ConvertFrom-Json
$parts = @{}
foreach ($part in $config.connectionString.Split(';')) {
    if ($part.Contains('=')) { $pair = $part.Split('=',2); $parts[$pair[0].Trim()] = $pair[1].Trim() }
}
$env:PGHOST=$parts['Host']; $env:PGPORT=$parts['Port']; $env:PGDATABASE=$parts['Database']
$env:PGUSER=$parts['Username']; $env:PGPASSWORD=$parts['Password']
$psql = 'C:/Program Files/PostgreSQL/18/bin/psql.exe'
$query = @'
SELECT 'Users', count(*), md5(coalesce(string_agg(row_to_json(t)::text, '' ORDER BY "Id"),'')) FROM "Users" t
UNION ALL SELECT 'Clients', count(*), md5(coalesce(string_agg(row_to_json(t)::text, '' ORDER BY "Id"),'')) FROM "Clients" t
UNION ALL SELECT 'Banks', count(*), md5(coalesce(string_agg(row_to_json(t)::text, '' ORDER BY "Id"),'')) FROM "Banks" t
UNION ALL SELECT 'BankAccounts', count(*), md5(coalesce(string_agg(row_to_json(t)::text, '' ORDER BY "Id"),'')) FROM "BankAccounts" t;
'@
try {
    $result = $query | & $psql -X -q -t -A -v ON_ERROR_STOP=1
    if ($LASTEXITCODE -ne 0) { throw 'Database verification failed' }
    $checkPath = Join-Path $root '.local/catalog-fingerprints.txt'
    if ($Phase -eq 'Before') { $result | Set-Content -Encoding UTF8 $checkPath }
    else {
        $before = Get-Content $checkPath
        if (Compare-Object $before $result) { throw 'Preserved catalog data changed unexpectedly' }
        Write-Output 'Preservation verified: all catalog rows, users, photos and logos are identical.'
    }
    $result | ForEach-Object { $columns=$_.Split('|'); '{0}: {1} rows' -f $columns[0],$columns[1] }
    'SELECT tablename FROM pg_tables WHERE schemaname=''public'' ORDER BY tablename;' | & $psql -X -q -t -A -v ON_ERROR_STOP=1
} finally { Remove-Item Env:PGPASSWORD -ErrorAction SilentlyContinue }
