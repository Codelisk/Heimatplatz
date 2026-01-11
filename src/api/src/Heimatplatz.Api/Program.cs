
using Heimatplatz.Api.Core.Data.Configuration;
using Heimatplatz.Api.Core.Startup;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddOpenApi();
builder.Services.AddApiServices(builder.Configuration);

var app = builder.Build();

app.Services.EnsureDatabaseCreated();
await app.RunSeedersAsync();

app.MapDefaultEndpoints();
app.MapEndpoints();

if (app.Environment.IsDevelopment())
{
    _ = app.MapOpenApi();
    _ = app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.Run();
