# syntax=docker/dockerfile:1
#
# WICHTIG: Build-Kontext MUSS das Repo-Root sein, NICHT src/api/ - Directory.Build.props und
# Directory.Packages.props unter src/api/ importieren per GetPathOfFileAbove die Root-Varianten
# (Central Package Management, TargetFramework). Aufruf vom Repo-Root aus:
#   docker build -f src/api/Dockerfile -t heimatplatz-api .
# (deploy/hetzner/docker-compose.yml setzt context/dockerfile bereits korrekt.)

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Root-Konfigurationsdateien zuerst kopieren (Docker-Layer-Caching: aendern sich selten)
COPY Directory.Build.props Directory.Packages.props nuget.config ./
COPY src/api/ src/api/

RUN dotnet restore src/api/src/Heimatplatz.Api/Heimatplatz.Api.csproj

# Framework-dependent Publish (nicht self-contained wie beim Azure-Deploy) - das aspnet-Runtime-
# Image liefert den .NET-Runtime bereits mit, self-contained waere hier nur totes Gewicht.
RUN dotnet publish src/api/src/Heimatplatz.Api/Heimatplatz.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

# Npgsql versucht beim Verbindungsaufbau optional GSSAPI/Kerberos-Auth und loggt sonst
# "Cannot load library libgssapi_krb5.so.2" (Verbindung funktioniert zwar trotzdem ueber den
# Fallback-Mechanismus, aber das Fehlerlog ist irritierend) - das schlanke aspnet-Image bringt die
# Bibliothek nicht mit.
RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Heimatplatz.Api.dll"]
