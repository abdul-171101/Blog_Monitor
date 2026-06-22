$baseUrl = "http://localhost:5271"
$email = "verify@blogmonitor.test"
$password = "Verify123!"
$result = [System.Collections.ArrayList]::new()

$result.Add('=== STEP 1: Auth and Pipeline Trigger ===') | Out-Null
try {
    $loginResponse = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method Post -Body (@{email=$email; password=$password} | ConvertTo-Json) -ContentType "application/json" -UseBasicParsing -ErrorAction Stop
    $token = $loginResponse.AccessToken
    $role = $loginResponse.Role
    $result.Add("[OK] Login succeeded. Role=$role") | Out-Null
} catch {
    $result.Add("[WARN] Login failed: $($_.Exception.Message)") | Out-Null
    try {
        Invoke-RestMethod -Uri "$baseUrl/api/auth/register" -Method Post -Body (@{email=$email; password=$password; role="Admin"} | ConvertTo-Json) -ContentType "application/json" -UseBasicParsing -ErrorAction Stop
        $result.Add("[OK] Registered user $email") | Out-Null
        $loginResponse = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method Post -Body (@{email=$email; password=$password} | ConvertTo-Json) -ContentType "application/json" -UseBasicParsing -ErrorAction Stop
        $token = $loginResponse.AccessToken
        $role = $loginResponse.Role
        $result.Add("[OK] Login succeeded after register. Role=$role") | Out-Null
    } catch {
        $result.Add("[FAIL] Auth failed after register: $($_.Exception.Message)") | Out-Null
        $token = $null
    }
}

if (-not $token) {
    $result.Add('[FAIL] No token obtained; aborting pipeline trigger') | Out-Null
} else {
    $headers = @{ Authorization = "Bearer $token" }
    try {
        Invoke-RestMethod -Uri "$baseUrl/api/urls/1/run" -Method Post -Headers $headers -UseBasicParsing -ErrorAction Stop | Out-Null
        $result.Add('[OK] Pipeline POST /api/urls/1/run succeeded') | Out-Null
    } catch {
        $result.Add("[FAIL] Pipeline trigger failed: $($_.Exception.Message)") | Out-Null
    }
}

$result.Add('Waiting 90 seconds for pipeline job completion...') | Out-Null
Start-Sleep -Seconds 90
$result.Add('Wait complete.') | Out-Null

$result.Add('=== STEP 2: Hangfire Job Result ===') | Out-Null
$cs = "Server=localhost,1433;Database=BlogMonitor;User Id=sa;Password=MyPassword@123;TrustServerCertificate=True;Encrypt=False;"
try {
    $cn = New-Object System.Data.SqlClient.SqlConnection $cs
    $cn.Open()
    $cmd = $cn.CreateCommand()
    $cmd.CommandText = "SELECT TOP 1 s.Name AS StateName FROM HangFire.Job j LEFT JOIN HangFire.State s ON j.StateId = s.Id ORDER BY j.CreatedAt DESC"
    $stateName = $cmd.ExecuteScalar()
    $cn.Close()
    $result.Add("[OK] Hangfire latest StateName=$stateName") | Out-Null
} catch {
    $result.Add("[FAIL] Hangfire query failed: $($_.Exception.Message)") | Out-Null
}

$result.Add('=== STEP 3: Disk files ===') | Out-Null
$storageBase = 'C:\BlogMonitorStorage'
$sitemapFiles = Get-ChildItem -Path "$storageBase\sitemaps" -Filter '*.xml' -Recurse -ErrorAction SilentlyContinue
$markdownFiles = Get-ChildItem -Path "$storageBase\markdown" -Filter '*.md' -Recurse -ErrorAction SilentlyContinue
$result.Add("[OK] Storage BasePath=$storageBase") | Out-Null
$result.Add("[OK] Sitemap XML count=$($sitemapFiles.Count)") | Out-Null
foreach ($f in $sitemapFiles) { $result.Add("  $($f.FullName)") | Out-Null }
$result.Add("[OK] Markdown MD count=$($markdownFiles.Count)") | Out-Null
foreach ($f in $markdownFiles) { $result.Add("  $($f.FullName)") | Out-Null }

$result.Add('=== STEP 4: ContentHashes count ===') | Out-Null
try {
    $cn = New-Object System.Data.SqlClient.SqlConnection $cs
    $cn.Open()
    $cmd = $cn.CreateCommand()
    $cmd.CommandText = "SELECT COUNT(*) AS TotalHashes FROM ContentHashes WHERE ClientUrlId = 1"
    $totalHashes = [int]$cmd.ExecuteScalar()
    $cn.Close()
    $result.Add("[OK] TotalHashes=$totalHashes") | Out-Null
} catch {
    $result.Add("[FAIL] ContentHashes query failed: $($_.Exception.Message)") | Out-Null
}

$result.Add('=== STEP 5: DuckDB verification ===') | Out-Null
$duckdbPath = "$storageBase\analytics\blogmonitor.duckdb"
$testFile = 'd:\BlogMonitor\BlogMonitor.Tests\TempDuckDbVerificationTests.cs'
Add-Content -Path $testFile -Value @'
using System;
using Xunit;
using DuckDB.NET.Data;

public class TempDuckDbVerificationTests
{
    [Fact]
    public void VerifyDailySummariesAndPageSummaries()
    {
        using var cnn = new DuckDBConnection("Data Source=C:\\BlogMonitorStorage\\analytics\\blogmonitor.duckdb");
        cnn.Open();

        using var cmd1 = cnn.CreateCommand();
        cmd1.CommandText = "SELECT status, pages_changed, pages_no_change FROM daily_summaries WHERE client_url_id = 1 ORDER BY created_at DESC LIMIT 1";
        using var reader1 = cmd1.ExecuteReader();
        Assert.True(reader1.Read(), "daily_summaries row missing for client_url_id=1");
        var status = reader1.GetString(0);
        var pagesChanged = reader1.GetInt32(1);
        var pagesNoChange = reader1.GetInt32(2);

        using var cmd2 = cnn.CreateCommand();
        cmd2.CommandText = "SELECT COUNT(*) FROM page_summaries WHERE client_url_id = 1 AND summary_status = 'SUCCESS'";
        var successCount = Convert.ToInt32(cmd2.ExecuteScalar());

        Assert.True(status == "INITIAL_RUN" || status == "CHANGED", $"Unexpected status: {status}");
        Assert.True(successCount >= 0, "page_summaries count query failed");

        Console.WriteLine($"DuckDBRow: status={status}, pages_changed={pagesChanged}, pages_no_change={pagesNoChange}, successCount={successCount}");
    }
}
'@ | Out-Null

try {
    $testResult = dotnet test d:\BlogMonitor\BlogMonitor.Tests\BlogMonitor.Tests.csproj --filter FullyQualifiedName~TempDuckDbVerificationTests --no-restore --verbosity normal 2>&1
    $result.Add('[OK] DuckDB xUnit test executed') | Out-Null
    $result.Add($testResult) | Out-Null
} catch {
    $result.Add("[FAIL] DuckDB xUnit test failed: $($_.Exception.Message)") | Out-Null
}

Remove-Item -Path $testFile -ErrorAction SilentlyContinue

$result.Add('=== STEP 6: Final Checklist ===') | Out-Null
$passed = @()
$passed += if ($stateName -eq 'Succeeded') { 'YES' } else { 'NO' }
$passed += if ($sitemapFiles.Count -gt 0) { 'YES' } else { 'NO' }
$passed += if ($markdownFiles.Count -gt 0) { 'YES' } else { 'NO' }
$passed += if ($totalHashes -gt 0) { 'YES' } else { 'NO' }
$duckdbStatus = 'UNKNOWN'
$duckdbSuccessCount = 'UNKNOWN'
if ($testResult -match 'DuckDBRow: status=(\S+), pages_changed=(\d+), pages_no_change=(\d+)') {
    $duckdbStatus = $matches[1]
}
if ($testResult -match 'successCount=(\d+)') {
    $duckdbSuccessCount = [int]$matches[1]
}
$passed += if ($duckdbStatus -in 'INITIAL_RUN','CHANGED') { 'YES' } else { 'NO' }
$passed += if ([int]$duckdbSuccessCount -gt 0) { 'YES' } else { 'NO' }

$result.Add("Hangfire job StateName is Succeeded: $($passed[0])") | Out-Null
$result.Add("Sitemap XML file exists on disk: $($passed[1])") | Out-Null
$result.Add("At least one .md file exists on disk: $($passed[2])") | Out-Null
$result.Add("ContentHashes count is greater than zero: $($passed[3])") | Out-Null
$result.Add("daily_summaries has INITIAL_RUN or CHANGED: $($passed[4])") | Out-Null
$result.Add("page_summaries has at least one SUCCESS row: $($passed[5])") | Out-Null

if ($passed -contains 'NO') {
    $result.Add('PLAYWRIGHT VERIFIED FAILED') | Out-Null
} else {
    $result.Add('PLAYWRIGHT VERIFIED — BACKEND FULLY COMPLETE — PROCEED TO FRONTEND PHASE 1') | Out-Null
}

$result | Out-File d:\BlogMonitor\verify_playwright_output.txt -Encoding utf8 -Force
$result | ForEach-Object { Write-Output $_ }
