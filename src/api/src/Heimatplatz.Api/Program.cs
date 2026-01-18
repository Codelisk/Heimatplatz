
using System.Text;
using System.Text.Json.Serialization;
using Heimatplatz.Api.Authorization;
using Heimatplatz.Api.Core.Data.Configuration;
using Heimatplatz.Api.Core.Startup;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddOpenApi();
builder.Services.AddApiServices(builder.Configuration);

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

app.Services.EnsureDatabaseCreated();
await app.RunSeedersAsync();

app.UseHttpsRedirection();

// Authentication und Authorization Middleware
app.UseAuthentication();
app.UseAuthorization();


app.MapDefaultEndpoints();
app.MapEndpoints();

if (app.Environment.IsDevelopment())
{
    _ = app.MapOpenApi();
    _ = app.MapScalarApiReference();
}

app.Run();
