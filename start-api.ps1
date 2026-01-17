$env:DOTNET_MODIFIABLE_ASSEMBLIES = "debug"
Set-Location "C:\Users\Daniel\source\repos\ai\projects\Heimatplatz\src\api\src\Heimatplatz.Api"
Start-Process -NoNewWindow -FilePath "dotnet" -ArgumentList "run","--project","Heimatplatz.Api.csproj"
