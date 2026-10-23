// Probe: UseConsoleLogging (source/timewarp-nuru/logging/nuru-logging-extensions.cs,
// NuruLoggingExtensions) is a public builder extension. A consumer must be able to call it and
// still run their own routes.
using Microsoft.Extensions.Logging;
using TimeWarp.Nuru;

NuruApp app = NuruApp.CreateBuilder()
  .UseConsoleLogging(LogLevel.Warning)
  .Map("ping")
    .WithHandler(() => "pong")
    .AsQuery()
    .Done()
  .Build();

return await app.RunAsync(args);
