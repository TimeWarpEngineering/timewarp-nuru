// Aspire Host with OpenTelemetry Collector Sample
// ================================================
// This sample demonstrates:
// - Aspire Host orchestrating an OpenTelemetry Collector and a Nuru CLI/REPL app
// - Telemetry (traces, metrics, structured logs) flowing to Aspire Dashboard
// - Proper structured logging with ILogger instead of Console.WriteLine
//
// To run:
//   dotnet run --project AspireHostOtel.AppHost
//
// Then open the Aspire Dashboard URL (printed to console) and interact with the NuruClient.

var builder = DistributedApplication.CreateBuilder(args);

// Add OpenTelemetry Collector with automatic forwarding
// This makes all resources send telemetry through the collector
var collector = builder.AddOpenTelemetryCollector("otel-collector")
  .WithAppForwarding(); // Auto-forward all resources to collector

// Add the Nuru CLI/REPL console app
builder.AddProject<Projects.AspireHostOtel_NuruClient>("nuru-client");

builder.Build().Run();
