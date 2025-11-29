// Aspire Host with OpenTelemetry Collector Sample
// ================================================
// This sample demonstrates:
// - Aspire Host orchestrating an OpenTelemetry Collector with Aspire Dashboard
// - External CLI apps sending telemetry via OTEL_EXPORTER_OTLP_ENDPOINT
//
// Architecture:
//   AppHost runs: Dashboard + OTEL Collector (in Docker)
//   User runs: NuruClient separately in their own terminal
//
// To run:
//   Terminal 1: dotnet run --project AspireHostOtel.AppHost --launch-profile http
//   Terminal 2: OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:19034 dotnet run --project AspireHostOtel.NuruClient

var builder = DistributedApplication.CreateBuilder(args);

// Add OpenTelemetry Collector with automatic forwarding
// This creates the OTLP endpoint that external apps can send telemetry to
builder.AddOpenTelemetryCollector("otel-collector")
  .WithAppForwarding();

// NOTE: The NuruClient is NOT orchestrated by Aspire - it runs in its own terminal.
// This is the correct pattern for CLI/REPL apps where the user needs direct console access.
// The NuruClient sends telemetry to the collector via OTEL_EXPORTER_OTLP_ENDPOINT.

DistributedApplication app = builder.Build();

// Print instructions for running the client
Console.WriteLine();
Console.WriteLine("=== Aspire Host with OpenTelemetry ===");
Console.WriteLine();
Console.WriteLine("The Aspire Dashboard and OTEL Collector are starting...");
Console.WriteLine();
Console.WriteLine("To run the NuruClient with telemetry, open a NEW terminal and run:");
Console.WriteLine();
Console.WriteLine("  cd Samples/AspireHostOtel");
Console.WriteLine("  OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:19034 dotnet run --project AspireHostOtel.NuruClient");
Console.WriteLine();
Console.WriteLine("Then interact with the REPL and watch telemetry appear in the Aspire Dashboard!");
Console.WriteLine();

app.Run();
