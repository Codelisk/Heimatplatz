$body = @{
    Email = "test.seller@heimatplatz.dev"
    Passwort = "Test123!"
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri "http://localhost:5292/api/auth/login" -Method Post -Body $body -ContentType "application/json"
    Write-Output "Login successful!"
    $response | ConvertTo-Json
} catch {
    Write-Output "Login failed: $($_.Exception.Message)"
    if ($_.Exception.Response) {
        $stream = $_.Exception.Response.GetResponseStream()
        $reader = New-Object System.IO.StreamReader($stream)
        Write-Output $reader.ReadToEnd()
    }
}
