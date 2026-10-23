// Probe (nuru#242): UseTelemetry options reach the exporter. The shipped TelemetryBehavior makes a
// span per command (telemetry.md); the check listens on the literal endpoint below and looks for
// the service name in what the app sends. The generator reads these options at compile time, so
// they must be literals, as the docs show them.
using TimeWarp.Nuru;

NuruApp app = NuruApp.CreateBuilder()
  .UseTelemetry(options =>
  {
    options.ServiceName = "nuru-probe-telemetry";
    options.OtlpEndpoint = "http://127.0.0.1:43177";
    options.EnableTracing = true;
    options.EnableMetrics = false;
    options.EnableLogging = false;
  })
  .AddBehavior(typeof(TelemetryBehavior))
  .Map("ping")
    .WithHandler(() => "pong")
    .AsQuery()
    .Done()
  .Build();

return await app.RunAsync(args);
