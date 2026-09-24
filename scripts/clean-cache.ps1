# Run with the application stopped. Dependencies and databases are preserved.
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Split-Path $PSScriptRoot -Parent))
$targets = @('frontend/Import-pdf-excel-master/.next', 'frontend/Import-pdf-excel-master/tsconfig.tsbuildinfo')
Get-ChildItem (Join-Path $root 'backend') -Directory | Where-Object { $_.Name -like 'BankStatementConverter.*' } | ForEach-Object {
    $targets += Join-Path $_.FullName 'bin'
    $targets += Join-Path $_.FullName 'obj'
}
foreach ($relative in $targets) {
    $target = if ([IO.Path]::IsPathRooted($relative)) { [IO.Path]::GetFullPath($relative) } else { [IO.Path]::GetFullPath((Join-Path $root $relative)) }
    if (!$target.StartsWith($root + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Path outside workspace' }
    if (Test-Path -LiteralPath $target) { Remove-Item -LiteralPath $target -Recurse -Force }
}
Write-Output 'Build caches removed. Source files, dependencies and databases preserved.'
