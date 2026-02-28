using System.Text;
using System.Text.Json.Serialization;
using Heimatplatz.Api;
using Heimatplatz.Api.Authorization;
using Heimatplatz.Api.Core.Data.Configuration;
using Heimatplatz.Api.Core.Startup;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddHttpClient("ImageProxy");
builder.Services.AddExceptionHandler<UnauthorizedExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddApiServices(builder.Configuration);

// CORS fuer Uno WASM Frontend
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// JWT Authentication konfigurieren
var jwtKey = builder.Configuration["Authentication:Jwt:Key"]
    ?? throw new InvalidOperationException("JWT Key nicht konfiguriert");
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

// Datenbank initialisieren (Migration + Seeding basierend auf DatabaseOptions)
await app.InitializeDatabaseAsync();

app.UseExceptionHandler();
app.UseCors();
app.UseStaticFiles();
app.UseHttpsRedirection();

// Authentication und Authorization Middleware
app.UseAuthentication();
app.UseAuthorization();

app.MapEndpoints();

// Image proxy endpoint - bypasses CORS for external image URLs (e.g. edikte.justiz.gv.at)
app.MapGet("/api/images/proxy", async (string url, IHttpClientFactory httpClientFactory, HttpContext ctx) =>
{
    if (string.IsNullOrEmpty(url) || !Uri.TryCreate(url, UriKind.Absolute, out var uri))
        return Results.BadRequest("Invalid URL");

    // Only allow HTTPS image URLs
    if (uri.Scheme != "https")
        return Results.BadRequest("Only HTTPS URLs allowed");

    try
    {
        var client = httpClientFactory.CreateClient("ImageProxy");
        using var response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead);
        if (!response.IsSuccessStatusCode)
            return Results.StatusCode((int)response.StatusCode);

        var contentType = response.Content.Headers.ContentType?.MediaType ?? "image/jpeg";
        var bytes = await response.Content.ReadAsByteArrayAsync();

        // Cache for 24 hours
        ctx.Response.Headers.CacheControl = "public, max-age=86400";
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
