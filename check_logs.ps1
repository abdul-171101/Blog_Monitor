$cs = "Server=localhost,1433;Database=BlogMonitor;User Id=sa;Password=MyPassword@123;TrustServerCertificate=True;Encrypt=False;"

Write-Output "=== LATEST HANGFIRE JOBS ==="
$cn = New-Object System.Data.SqlClient.SqlConnection $cs
$cn.Open()
$cmd = $cn.CreateCommand()
$cmd.CommandText = "SELECT TOP 5 j.Id, s.Name as State, j.CreatedAt FROM HangFire.Job j LEFT JOIN HangFire.State s ON j.StateId = s.Id ORDER BY j.Id DESC"
$reader = $cmd.ExecuteReader()
while ($reader.Read()) {
  Write-Output "Job ID: $($reader[0]), State: $($reader[1]), Created: $($reader[2])"
}
$reader.Close()
$cn.Close()

Write-Output "`n=== LATEST MONITORING LOGS (20 most recent) ==="
$cn = New-Object System.Data.SqlClient.SqlConnection $cs
$cn.Open()
$cmd = $cn.CreateCommand()
$cmd.CommandText = "SELECT TOP 20 Timestamp, Level, Message FROM MonitoringLogs ORDER BY Timestamp DESC"
$dt = New-Object System.Data.DataTable
$dt.Load($cmd.ExecuteReader())
$cn.Close()

foreach ($row in $dt.Rows) {
  Write-Output "$($row[0]) [$($row[1])] $($row[2])"
}

Write-Output "`n=== MonitoringLogs for ClientUrl 2 ==="
$cn = New-Object System.Data.SqlClient.SqlConnection $cs
$cn.Open()
$cmd = $cn.CreateCommand()
$cmd.CommandText = "SELECT TOP 20 Timestamp, Level, Message FROM MonitoringLogs WHERE ClientUrlId = 2 ORDER BY Timestamp DESC"
$dt = New-Object System.Data.DataTable
$dt.Load($cmd.ExecuteReader())
$cn.Close()

foreach ($row in $dt.Rows) {
  Write-Output "$($row[0]) [$($row[1])] $($row[2])"
}
