$body = @{
    email = "test.seller@heimatplatz.dev"
    password = "Test123!"
} | ConvertTo-Json

$response = Invoke-WebRequest -Uri "http://localhost:5292/api/auth/login" -Method POST -Body $body -ContentType "application/json"
$response.Content
