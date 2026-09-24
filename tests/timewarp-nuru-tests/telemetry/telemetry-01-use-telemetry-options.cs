#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj

#region Purpose
// Kanban 470-001: UseTelemetry(Action<NuruTelemetryOptions>) applies Enable* flags and OtlpEndpoint.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Telemetry
{

[TestTag("Telemetry")]
public class UseTelemetryOptionsTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<UseTelemetryOptionsTests>();

  public static async Task Should_apply_otlp_endpoint_and_enable_flags()
  {
    using TestTerminal terminal = new();
    Tel01Capture.Reset();

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .UseTelemetry(options =>
      {
        options.OtlpEndpoint = "http://127.0.0.1:9";
        options.EnableTracing = true;
        options.EnableMetrics = false;
        options.EnableLogging = false;
        options.ServiceName = "tel01-cli";
      })
      .Map("tel01-probe")
        .WithHandler(Tel01Capture.Probe)
        .AsQuery()
        .Done()
      .Build();

    Tel01Capture.App = app;
    int exitCode = await app.RunAsync(["tel01-probe"]);

    exitCode.ShouldBe(0);
    terminal.OutputContains("ok").ShouldBeTrue();
    Tel01Capture.TracerWasSet.ShouldBe(true);
    Tel01Capture.MeterWasSet.ShouldBe(false);
  }
}

}

public static class Tel01Capture
{
  public static NuruApp? App { get; set; }
  public static bool? TracerWasSet { get; set; }
  public static bool? MeterWasSet { get; set; }

  public static void Reset()
  {
    App = null;
    TracerWasSet = null;
    MeterWasSet = null;
  }

  public static string Probe()
  {
    TracerWasSet = App?.TracerProvider is not null;
    MeterWasSet = App?.MeterProvider is not null;
    return "ok";
  }
}
