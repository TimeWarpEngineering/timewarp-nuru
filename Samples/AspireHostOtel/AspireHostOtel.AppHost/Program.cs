// Aspire Host with OpenTelemetry Sample
// ======================================
// This sample demonstrates:
// - Aspire Dashboard with built-in OTLP receiver for telemetry
// - NuruClient registered as an Aspire-managed project
// - Telemetry flows automatically to the Aspire Dashboard
//
// To run:
//   dotnet run
//   (Aspire launches NuruClient automatically)

var builder = DistributedApplication.CreateBuilder(args);

// Register NuruClient as an Aspire-managed project.
// Aspire will:
// - Launch it automatically
// - Inject OTEL_EXPORTER_OTLP_ENDPOINT pointing to the dashboard
// - Show its telemetry in the dashboard
//
// Pass arguments to run a command instead of entering REPL mode
// (REPL requires interactive console which Aspire doesn't provide)
builder.AddProject<Projects.AspireHostOtel_NuruClient>("nuruclient")
  .WithArgs("status");

DistributedApplication app = builder.Build();
app.Run();
