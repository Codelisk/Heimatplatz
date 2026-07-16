using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Heimatplatz.Api;
using Heimatplatz.Api.Authorization;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Core.Data.Configuration;
using Heimatplatz.Api.Core.Startup;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using SkiaSharp;

var builder = WebApplication.CreateBuilder(args);

// Groessere Request-Bodies fuer Base64-Video-Uploads der KI-Inseratserstellung
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 150 * 1024 * 1024; // 150 MB
});

builder.Services.AddOpenApi();

// SSRF-Schutz: Redirects NIE automatisch folgen. Der Endpoint prueft jeden Redirect-Hop manuell
// erneut gegen die Host-Allow-List (siehe /api/images/proxy) - ohne diese Einstellung wuerde ein
// Open-Redirect auf einem erlaubten Host (z.B. picsum.photos ist per Design ein Redirect-Dienst)
// beliebige, nicht freigegebene Ziel-Hosts (inkl. interner Endpunkte) proxybar machen.
builder.Services.AddHttpClient("ImageProxy")
    .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
    {
        AllowAutoRedirect = false,
        MaxConnectionsPerServer = 20
    });
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddExceptionHandler<UnauthorizedExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddApiServices(builder.Configuration);

// CORS fuer das Astro-Web-Frontend (Browser-Cross-Origin-Requests)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Hinter Azure App Service Frontends: echte Client-IP und Schema aus X-Forwarded-* uebernehmen,
// sonst sieht der Rate-Limiter nur die Frontend-IP. KnownIPNetworks/KnownProxies leeren, weil
// die Azure-Frontend-IPs nicht statisch bekannt sind (App Service Standard-Setup).
// ForwardLimit=1 explizit setzen: nur der RECHTESTE (vom Frontend angehaengte) XFF-Eintrag wird
// uebernommen. Die Sicherheit dieses Setups haengt davon ab, dass die API nur hinter einem
// Reverse-Proxy erreichbar ist, der die echte Client-IP selbst anhaengt (Azure App Service
// Standard-Verhalten) - laeuft die API direkt exponiert (lokal, Docker ohne Proxy), kann jeder
// Client per X-Forwarded-For seine RemoteIpAddress frei waehlen und damit Rate-Limits umgehen.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.ForwardLimit = 1;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

// Rate-Limiting pro Client-IP: Auth-Endpunkte streng (Brute-Force-Schutz), alle anderen Pfade
// grosszuegig (Image-Proxy-Thumbnails erzeugen viele parallele Requests). GlobalLimiter, weil
// die Shiny.Mediator-generierten Endpunkte via MapEndpoints() keine per-Endpoint-Policies erlauben.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        var path = context.Request.Path;

        // Azure-Health-Probes: kein Freifahrtschein fuer beliebige Aufrufer, aber deutlich
        // grosszuegiger als der Default-Bucket, damit ein Angreifer den DB-Health-Check nicht
        // unlimitiert als DoS-Verstaerker gegen die Datenbank missbrauchen kann.
        if (path.StartsWithSegments("/health"))
        {
            var healthIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            return RateLimitPartition.GetFixedWindowLimiter($"health:{healthIp}", _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 60,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            });
        }

        var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        // /api/auth/refresh bewusst NICHT im strengen Bucket: der Refresh ist bereits durch den
        // Besitz eines gueltigen Refresh-Tokens geschuetzt, und mehrere Nutzer hinter derselben
        // Carrier-/CGNAT-IP (z.B. oesterreichische Mobilfunker) machen pro App-Start je einen
        // Session-Restore-Refresh - im 10/min-Login-Bucket wuerden sie sich gegenseitig aussperren.
        if (path.StartsWithSegments("/api/auth/login")
            || path.StartsWithSegments("/api/auth/register"))
        {
            return RateLimitPartition.GetFixedWindowLimiter($"auth:{clientIp}", _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            });
        }

        if (path.StartsWithSegments("/api/auth/refresh"))
        {
            return RateLimitPartition.GetFixedWindowLimiter($"refresh:{clientIp}", _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 60,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            });
        }

        return RateLimitPartition.GetFixedWindowLimiter($"default:{clientIp}", _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 300,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        });
    });
});

// Die Build-Zeit-OpenAPI-Generierung (GetDocument.Insider) fuehrt Program.cs mit aus, laeuft aber
// ohne konfigurierten JWT Key - dort Platzhalter statt Crash (offizielles ASP.NET-Core-Muster:
// Runtime-Codepfade per Entry-Assembly-Check einschraenken).
var isBuildTimeOpenApiGen = Assembly.GetEntryAssembly()?.GetName().Name == "GetDocument.Insider";

// Produktions-Guard: ein fehlender Connection-String darf NIE still auf die InMemory-Datenbank
// zurueckfallen (AddAppData tut das fuer die Build-Zeit-OpenAPI-Generierung). Geht die App-Service-
// Einstellung ConnectionStrings__DefaultConnection verloren (z.B. bei einer Neu-Provisionierung),
// wuerde die API sonst klaglos mit einer leeren In-Memory-DB starten und /health weiterhin
// "Healthy" melden - Nutzer sehen eine leere Plattform, alle Daten sind nach dem naechsten
// Neustart weg, unbemerkt vom Monitoring. Ausnahme "Testing": die Integration-Test-Factory
// verlaesst sich bewusst auf den InMemory-Fallback (appsettings.Testing.json).
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (!isBuildTimeOpenApiGen
    && !builder.Environment.IsDevelopment()
    && !builder.Environment.IsEnvironment("Testing")
    && string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Connection String 'DefaultConnection' nicht konfiguriert. Umgebungsvariable " +
        "'ConnectionStrings__DefaultConnection' setzen - ohne echte Datenbank wuerde die API " +
        "sonst still auf eine leere InMemory-Datenbank zurueckfallen.");
}

// JWT Authentication konfigurieren
var jwtKey = builder.Configuration["Authentication:Jwt:Key"]
    ?? (isBuildTimeOpenApiGen
        ? "BuildTime-Platzhalter-Key-Nur-Fuer-OpenAPI-Generierung!"
        : throw new InvalidOperationException(
            "JWT Key nicht konfiguriert. Umgebungsvariable 'Authentication__Jwt__Key' setzen " +
            "(zufaelliger Wert mit mindestens 32 Zeichen)."));

// Produktions-Guard: bekannte/oeffentliche Keys (der alte kompromittierte Standard-Key und der
// in appsettings.Development.json git-getrackte Dev-Key) sowie zu kurze Keys duerfen nie live gehen.
var knownInsecureJwtKeys = new[]
{
    "HeimatplatzSecureJwtSigningKey2025!MinLength32Chars",
    "DevOnly-NichtFuerProduktion-Heimatplatz-JwtSigningKey-LokaleEntwicklung2026"
};

if (!isBuildTimeOpenApiGen
    && !builder.Environment.IsDevelopment()
    && (knownInsecureJwtKeys.Contains(jwtKey) || jwtKey.Length < 32))
{
    throw new InvalidOperationException(
        "Unsicherer JWT Key: der kompromittierte Standard-Key, der oeffentliche Dev-Key bzw. Keys " +
        "unter 32 Zeichen sind ausserhalb von Development nicht erlaubt. Umgebungsvariable " +
        "'Authentication__Jwt__Key' mit einem neuen zufaelligen Key setzen.");
}

var jwtIssuer = builder.Configuration["Authentication:Jwt:Issuer"];
var jwtAudience = builder.Configuration["Authentication:Jwt:Audience"];

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    // Deaktiviere Claim-Type-Mapping (JWT Claims wie 'sub' nicht in XML-Schema Claims umwandeln)
    options.MapInboundClaims = false;

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.Zero // Keine Toleranz fuer Token-Ablauf
    };
});

// Authorization mit Policies fuer Benutzerrollen
builder.Services.AddAuthorization(options =>
{
    // Policy: Nur Kaeufer
    options.AddPolicy(AuthorizationPolicies.RequireBuyer, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("user_role", "Buyer");
    });

    // Policy: Nur Verkaeufer
    options.AddPolicy(AuthorizationPolicies.RequireSeller, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("user_role", "Seller");
    });

    // Policy: Kaeufer ODER Verkaeufer (mindestens eine Rolle)
    options.AddPolicy(AuthorizationPolicies.RequireAnyRole, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireAssertion(context =>
            context.User.HasClaim(c =>
                c.Type == "user_role" &&
                (c.Value == "Buyer" || c.Value == "Seller")));
    });

    // Policy: Kaeufer UND Verkaeufer (beide Rollen)
    options.AddPolicy(AuthorizationPolicies.RequireBuyerAndSeller, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("user_role", "Buyer");
        policy.RequireClaim("user_role", "Seller");
    });
});

// JSON Serialization: Enums als Strings fuer bessere OpenAPI Dokumentation
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    // PascalCase fuer JSON-Properties verwenden (um mit C# Records zu matchen)
    options.SerializerOptions.PropertyNamingPolicy = null;
});

var app = builder.Build();

// Transparenz-Hinweis: der Mock-Provider liefert nur Platzhalter statt echter KI-Extraktion
if (!app.Environment.IsDevelopment() && app.Configuration["AiListing:Provider"] == "Mock")
{
    app.Logger.LogWarning(
        "AiListing laeuft im Mock-Modus: Die KI-Inseratserstellung liefert nur Platzhalter-Daten. " +
        "Fuer echte Extraktion AiListing__Provider=AiConnector (plus AiConnector__ApiKey) " +
        "oder AiListing__Provider=Cli setzen.");
}

// Datenbank initialisieren (Migration + Seeding basierend auf DatabaseOptions)
await app.InitializeDatabaseAsync();

// Muss als erste Middleware laufen, damit alle nachfolgenden (Rate-Limiter, HTTPS-Redirect)
// die echte Client-IP bzw. das echte Schema sehen
app.UseForwardedHeaders();

app.UseExceptionHandler();
app.UseCors();
app.UseRateLimiter();

// HSTS/HTTPS-Redirect MUESSEN vor UseStaticFiles laufen: Static Files (wwwroot/uploads - dort
// liegen die hochgeladenen Property-Bilder) wuerden die Pipeline sonst VOR diesen Middlewares
// kurzschliessen, d.h. Plain-HTTP-Requests auf /uploads/... wuerden unverschluesselt ausgeliefert
// und nie den Strict-Transport-Security-Header tragen.
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// Authentication und Authorization Middleware
app.UseAuthentication();
app.UseAuthorization();

app.MapEndpoints();

// Health-Endpoint fuer Monitoring, Azure-Probes und Integrationstests: prueft die DB-Erreichbarkeit
app.MapGet("/health", async (AppDbContext dbContext, CancellationToken cancellationToken) =>
{
    try
    {
        // 5s-Timeout, damit eine haengende DB-Verbindung die Probe nicht blockiert
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(5));

        var canConnect = await dbContext.Database.CanConnectAsync(timeoutCts.Token);
        return canConnect
            ? Results.Ok(new { Status = "Healthy" })
            : Results.Json(new { Status = "Unhealthy" }, statusCode: StatusCodes.Status503ServiceUnavailable);
    }
    catch
    {
        // Timeout oder Provider-Fehler (z.B. Build-Zeit/OpenAPI-Generierung ohne Connection-String)
        // -> sauber Unhealthy melden statt zu crashen
        return Results.Json(new { Status = "Unhealthy" }, statusCode: StatusCodes.Status503ServiceUnavailable);
    }
}).ExcludeFromDescription();

// SSRF-Schutz: nur explizit erlaubte Bild-Hosts (Config "ImageProxy:AllowedHosts") proxien
var allowedImageHosts = app.Configuration.GetSection("ImageProxy:AllowedHosts").Get<string[]>() ?? [];

// Harte Obergrenze fuer den gepufferten Bild-Body: verhindert Memory-Exhaustion, falls ein
// (erlaubter oder per Redirect erreichter) Host Gigabytes an Daten streamt.
const long maxImageProxyBytes = 20 * 1024 * 1024; // 20 MB

// Image proxy endpoint - bypasses CORS for external image URLs (e.g. edikte.justiz.gv.at)
// Optionaler ?w= Parameter skaliert serverseitig auf die Zielbreite herunter (nie hoch), damit
// Listen-Thumbnails nicht die oft mehrere MB grossen Originalbilder ungeskaliert an den Client schicken.
app.MapGet("/api/images/proxy", async (string url, int? w, IHttpClientFactory httpClientFactory, HttpContext ctx) =>
{
    if (string.IsNullOrEmpty(url) || !Uri.TryCreate(url, UriKind.Absolute, out var uri))
        return Results.BadRequest("Invalid URL");

    if (!IsImageUrlAllowed(uri, allowedImageHosts))
        return Results.BadRequest("Host not allowed");

    try
    {
        var client = httpClientFactory.CreateClient("ImageProxy");

        // SSRF-Schutz: der HttpClient folgt Redirects NICHT automatisch (AllowAutoRedirect=false).
        // Jeder Redirect-Hop wird hier manuell erneut gegen Schema/Port/Allow-List geprueft, bevor
        // ihm gefolgt wird - sonst koennte ein Open-Redirect auf einem erlaubten Host (z.B.
        // picsum.photos ist per Design ein Redirect-Dienst) beliebige nicht freigegebene Ziele
        // (inkl. interner Endpunkte) proxybar machen.
        const int maxRedirects = 5;
        var currentUri = uri;
        HttpResponseMessage response;
        var redirectCount = 0;

        while (true)
        {
            response = await client.GetAsync(currentUri, HttpCompletionOption.ResponseHeadersRead);

            if (!IsRedirectStatusCode(response.StatusCode))
                break;

            var location = response.Headers.Location;
            response.Dispose();

            if (location is null || ++redirectCount > maxRedirects)
                return Results.StatusCode(502);

            currentUri = location.IsAbsoluteUri ? location : new Uri(currentUri, location);

            if (!IsImageUrlAllowed(currentUri, allowedImageHosts))
                return Results.BadRequest("Redirect target host not allowed");
        }

        using var _ = response;

        if (!response.IsSuccessStatusCode)
            return Results.StatusCode((int)response.StatusCode);

        // Content-Type auf Bilder einschraenken: der Proxy soll keine beliebigen Inhalte
        // (z.B. text/html eines missbrauchten Ziels) 1:1 auf der eigenen Origin ausliefern.
        var contentType = response.Content.Headers.ContentType?.MediaType ?? "image/jpeg";
        if (!contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            return Results.BadRequest("Unsupported content type");

        if (response.Content.Headers.ContentLength is > maxImageProxyBytes)
            return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);

        byte[] bytes;
        await using (var responseStream = await response.Content.ReadAsStreamAsync())
        await using (var buffered = new MemoryStream())
        {
            // Groessenlimit waehrend des Streamings durchsetzen, falls Content-Length fehlt oder falsch ist.
            var chunk = new byte[81920];
            int read;
            while ((read = await responseStream.ReadAsync(chunk)) > 0)
            {
                if (buffered.Length + read > maxImageProxyBytes)
                    return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);

                await buffered.WriteAsync(chunk.AsMemory(0, read));
            }

            bytes = buffered.ToArray();
        }

        // Cache for 24 hours
        ctx.Response.Headers.CacheControl = "public, max-age=86400";

        if (w is > 0 and <= 2000)
        {
            try
            {
                using var original = SKBitmap.Decode(bytes);
                if (original is not null && original.Width > w)
                {
                    var newHeight = (int)Math.Round(original.Height * (w.Value / (double)original.Width));
                    using var resized = original.Resize(
                        new SKSizeI(w.Value, newHeight),
                        new SKSamplingOptions(SKCubicResampler.Mitchell));
                    if (resized is not null)
                    {
                        using var image = SKImage.FromBitmap(resized);
                        using var data = image.Encode(SKEncodedImageFormat.Jpeg, 85);
                        return Results.File(data.ToArray(), "image/jpeg");
                    }
                }
            }
            catch
            {
                // Dekodierung fehlgeschlagen (z.B. unbekanntes Format) - Original unskaliert ausliefern
            }
        }

        return Results.File(bytes, contentType);
    }
    catch
    {
        return Results.StatusCode(502);
    }
}).ExcludeFromDescription();

if (app.Environment.IsDevelopment())
{
    _ = app.MapOpenApi();
    _ = app.MapScalarApiReference();
}

app.Run();

// Prueft einen Bild-Host gegen die Allow-List: Eintraege mit fuehrendem Punkt (z.B. ".justiz.gv.at")
// matchen als Suffix (beliebige Subdomains), alle anderen Eintraege exakt.
static bool IsAllowedImageHost(string host, string[] allowedHosts)
{
    foreach (var allowed in allowedHosts)
    {
        var isMatch = allowed.StartsWith('.')
            ? host.EndsWith(allowed, StringComparison.OrdinalIgnoreCase)
            : string.Equals(host, allowed, StringComparison.OrdinalIgnoreCase);

        if (isMatch)
            return true;
    }

    return false;
}

// Voll-Validierung einer Bild-URL (Schema, Standard-Port, Allow-List-Host). Wird sowohl fuer die
// urspruengliche Anfrage als auch fuer jeden Redirect-Hop aufgerufen - ein Redirect darf nur auf
// eine ebenso gueltige, erlaubte URL zeigen.
static bool IsImageUrlAllowed(Uri uri, string[] allowedHosts) =>
    uri.Scheme == "https" && uri.IsDefaultPort && IsAllowedImageHost(uri.Host, allowedHosts);

// 3xx-Redirect-Statuscodes, denen der Proxy manuell (mit erneuter Allow-List-Pruefung) folgt.
static bool IsRedirectStatusCode(System.Net.HttpStatusCode statusCode) => statusCode is
    System.Net.HttpStatusCode.MovedPermanently or
    System.Net.HttpStatusCode.Found or
    System.Net.HttpStatusCode.SeeOther or
    System.Net.HttpStatusCode.TemporaryRedirect or
    System.Net.HttpStatusCode.PermanentRedirect;
