Write-Host "Stage 14 - Disk Files Check"
$paths = @('C:\BlogMonitorStorage\sitemaps', 'C:\BlogMonitorStorage\markdown', 'C:\BlogMonitorStorage\reports')
foreach ($p in $paths) {
    if (Test-Path $p) {
        $files = @(Get-ChildItem -Path $p -Recurse -File -ErrorAction SilentlyContinue)
        $count = $files.Count
        Write-Host "$p : $count files"
        if ($count -gt 0) {
            $latest = $files | Sort-Object LastWriteTime -Descending | Select-Object -First 1
            Write-Host "  Latest: $($latest.Name) ($($latest.LastWriteTime))"
        }
    } else {
        Write-Host "$p : (does not exist)"
    }
}
