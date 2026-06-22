$body = @{
    email = 'debugtest@blogmonitor.test'
    password = 'Debug123!'
    role = 'Admin'
} | ConvertTo-Json -Depth 3

Write-Host '=== POST /api/auth/register ==='
try {
    $response = Invoke-RestMethod -Uri 'http://localhost:5271/api/auth/register' -Method Post -Body $body -ContentType 'application/json' -ErrorAction Stop
    Write-Host 'STATUS: 201'
    $response | ConvertTo-Json -Compress | Write-Output
} catch {
    if ($_.Exception.Response -ne $null) {
        $resp = $_.Exception.Response
        $reader = New-Object System.IO.StreamReader($resp.GetResponseStream())
        $text = $reader.ReadToEnd()
        Write-Host "HTTP_STATUS: $($resp.StatusCode.Value__)"
        Write-Host "STATUS_DESCRIPTION: $($resp.StatusDescription)"
        Write-Host 'BODY:'
        Write-Host $text
    } else {
        Write-Host 'ERROR:'
        Write-Host $_.Exception.Message
    }
    exit 1
}
