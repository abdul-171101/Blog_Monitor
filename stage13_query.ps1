$connStr = 'Server=localhost,1433;Database=BlogMonitor;User Id=sa;Password=MyPassword@123;TrustServerCertificate=true;Encrypt=false;'
$conn = New-Object System.Data.SqlClient.SqlConnection($connStr)

try {
    $conn.Open()
    Write-Host "Connected to BlogMonitor database"
    
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = 'SELECT COUNT(*) FROM Clients'
    Write-Host "Clients: $($cmd.ExecuteScalar())"
    
    $cmd.CommandText = 'SELECT COUNT(*) FROM ClientUrls'
    Write-Host "ClientUrls: $($cmd.ExecuteScalar())"
    
    $cmd.CommandText = 'SELECT COUNT(*) FROM ContentHashes'
    Write-Host "ContentHashes: $($cmd.ExecuteScalar())"
    
    $cmd.CommandText = 'SELECT COUNT(*) FROM MonitoringLogs'
    Write-Host "MonitoringLogs: $($cmd.ExecuteScalar())"
    
    Write-Host "Hangfire Recent Job States:"
    $cmd.CommandText = 'SELECT TOP 3 s.Name FROM HangFire.Job j LEFT JOIN HangFire.State s ON j.StateId = s.Id ORDER BY j.CreatedAt DESC'
    $reader = $cmd.ExecuteReader()
    $i = 0
    while ($reader.Read()) {
        if ($reader.IsDBNull(0)) {
            Write-Host "  [$i] (no state)"
        } else {
            Write-Host "  [$i] $($reader.GetString(0))"
        }
        $i = $i + 1
    }
    $reader.Close()
    
    $conn.Close()
    Write-Host "Stage 13 Complete"
}
catch {
    Write-Host "Error: $_"
}
