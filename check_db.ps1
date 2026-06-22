$cs = "Server=localhost,1433;Database=BlogMonitor;User Id=sa;Password=MyPassword@123;TrustServerCertificate=True;Encrypt=False;"
$cn = New-Object System.Data.SqlClient.SqlConnection $cs
$cn.Open()
$cmd = $cn.CreateCommand()
$cmd.CommandText = "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' ORDER BY TABLE_NAME"
$reader = $cmd.ExecuteReader()
Write-Output "=== DATABASE TABLES ==="
while ($reader.Read()) {
  Write-Output $reader['TABLE_NAME']
}
$cn.Close()

Write-Output "`n=== CHECKING EF_MIGRATIONS_HISTORY ==="
$cn = New-Object System.Data.SqlClient.SqlConnection $cs
$cn.Open()
$cmd = $cn.CreateCommand()
$cmd.CommandText = "SELECT * FROM __EFMigrationsHistory"
try {
  $reader = $cmd.ExecuteReader()
  while ($reader.Read()) {
    Write-Output "Migration: $($reader['MigrationId'])"
  }
  $cn.Close()
} catch {
  Write-Output "No migrations table found"
}
