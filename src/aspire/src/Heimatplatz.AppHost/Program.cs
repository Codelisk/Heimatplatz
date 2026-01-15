var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject<Projects.Heimatplatz_Api>("api");

// Note: The Uno Platform App (Heimatplatz.App) is referenced in the project file
// but not added here as an Aspire resource because it's a client application,
// not a service that can be orchestrated by Aspire.
// The App connects to the API using service discovery via the API's endpoint URL.

builder.Build().Run();
