// Aspire Host with OpenTelemetry Sample
// ======================================
// This sample demonstrates:
// - Aspire Dashboard with built-in OTLP receiver for telemetry
// - External CLI apps sending telemetry directly to the Dashboard
//
// Architecture:
//   AppHost runs: Aspire Dashboard (with built-in OTLP receiver on port 19034)
//   User runs: NuruClient separately in their own terminal
//
// To run:
//   Terminal 1: dotnet run --launch-profile http
//   Terminal 2: dotnet run --project ../AspireHostOtel.NuruClient --launch-profile AppHost

var builder = DistributedApplication.CreateBuilder(args);

// The Aspire Dashboard has a built-in OTLP receiver.
// No need for a separate OpenTelemetry Collector for simple scenarios.
// External apps send telemetry to DOTNET_DASHBOARD_OTLP_ENDPOINT_URL (port 19034 for http profile).

DistributedApplication app = builder.Build();

// Print instructions for running the client
Console.WriteLine();
Console.WriteLine("=== Aspire Host with OpenTelemetry ===");
Console.WriteLine();
Console.WriteLine("The Aspire Dashboard is starting with built-in OTLP receiver...");
Console.WriteLine("Dashboard OTLP endpoint: http://localhost:19034");
Console.WriteLine();
Console.WriteLine("To run the NuruClient with telemetry, open a NEW terminal and run:");
Console.WriteLine();
Console.WriteLine("  cd Samples/AspireHostOtel/AspireHostOtel.NuruClient");
Console.WriteLine("  dotnet run --launch-profile AppHost");
Console.WriteLine();
Console.WriteLine("Or manually set the endpoint:");
Console.WriteLine("  $env:OTEL_EXPORTER_OTLP_ENDPOINT = 'http://localhost:19034'");
Console.WriteLine("  dotnet run");
Console.WriteLine();
Console.WriteLine("Then interact with the REPL and watch telemetry appear in the Aspire Dashboard!");
Console.WriteLine();

app.Run();
