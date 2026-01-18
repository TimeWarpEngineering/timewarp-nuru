// Emitter that generates OpenTelemetry infrastructure when UseTelemetry() is called.
//
// When telemetry is enabled, this emitter generates:
// - ActivitySource and Meter static fields
// - Counter and Histogram metrics instruments
// - TracerProvider and MeterProvider setup code
// - Command execution instrumentation helpers

namespace TimeWarp.Nuru.Generators;

using System.Text;

/// <summary>
/// Emits OpenTelemetry infrastructure code when telemetry is enabled.
/// </summary>
internal static class TelemetryEmitter
{
  /// <summary>
  /// Emits the telemetry infrastructure fields (ActivitySource, Meter, counters, histograms).
  /// </summary>
  public static void EmitTelemetryFields(StringBuilder sb)
  {
    sb.AppendLine("  // ═══════════════════════════════════════════════════════════════════════════════");
    sb.AppendLine("  // TELEMETRY INFRASTRUCTURE (generated because UseTelemetry() was called)");
    sb.AppendLine("  // ═══════════════════════════════════════════════════════════════════════════════");
    sb.AppendLine();
    sb.AppendLine("  private static readonly global::System.Diagnostics.ActivitySource __activitySource = new(\"TimeWarp.Nuru\", \"1.0.0\");");
    sb.AppendLine("  private static readonly global::System.Diagnostics.Metrics.Meter __meter = new(\"TimeWarp.Nuru\", \"1.0.0\");");
    sb.AppendLine("  private static readonly global::System.Diagnostics.Metrics.Counter<int> __commandsInvoked = __meter.CreateCounter<int>(\"nuru.commands.invoked\", \"{commands}\", \"Number of commands executed\");");
    sb.AppendLine("  private static readonly global::System.Diagnostics.Metrics.Counter<int> __commandsErrored = __meter.CreateCounter<int>(\"nuru.commands.errors\", \"{errors}\", \"Number of failed commands\");");
    sb.AppendLine("  private static readonly global::System.Diagnostics.Metrics.Histogram<double> __commandDuration = __meter.CreateHistogram<double>(\"nuru.commands.duration\", \"ms\", \"Command execution duration in milliseconds\");");
    sb.AppendLine();
  }

  /// <summary>
  /// Emits the telemetry provider setup code.
  /// This should be called at the start of RunAsync_Intercepted to set up OTLP exporters.
  /// </summary>
  public static void EmitTelemetrySetup(StringBuilder sb)
  {
    sb.AppendLine("    // Setup telemetry providers if OTEL endpoint is configured");
    sb.AppendLine("    string? __otlpEndpoint = global::System.Environment.GetEnvironmentVariable(\"OTEL_EXPORTER_OTLP_ENDPOINT\");");
    sb.AppendLine("    if (!string.IsNullOrEmpty(__otlpEndpoint))");
    sb.AppendLine("    {");
    sb.AppendLine("      string __serviceName = global::System.Environment.GetEnvironmentVariable(\"OTEL_SERVICE_NAME\")");
    sb.AppendLine("        ?? global::System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name");
    sb.AppendLine("        ?? \"nuru-app\";");
    sb.AppendLine("      string? __serviceVersion = global::System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version?.ToString();");
    sb.AppendLine();
    sb.AppendLine("      global::OpenTelemetry.Resources.ResourceBuilder __resource = global::OpenTelemetry.Resources.ResourceBuilder.CreateDefault()");
    sb.AppendLine("        .AddService(serviceName: __serviceName, serviceVersion: __serviceVersion);");
    sb.AppendLine();
    sb.AppendLine("      global::System.Uri __endpoint = new(__otlpEndpoint);");
    sb.AppendLine();
    sb.AppendLine("      app.TracerProvider = global::OpenTelemetry.Sdk.CreateTracerProviderBuilder()");
    sb.AppendLine("        .SetResourceBuilder(__resource)");
    sb.AppendLine("        .AddSource(__activitySource.Name)");
    sb.AppendLine("        .AddSource(\"TimeWarp.Nuru.Behavior\") // For TelemetryBehavior");
    sb.AppendLine("        .AddOtlpExporter(o => o.Endpoint = __endpoint)");
    sb.AppendLine("        .Build();");
    sb.AppendLine();
    sb.AppendLine("      app.MeterProvider = global::OpenTelemetry.Sdk.CreateMeterProviderBuilder()");
    sb.AppendLine("        .SetResourceBuilder(__resource)");
    sb.AppendLine("        .AddMeter(__meter.Name)");
    sb.AppendLine("        .AddMeter(\"TimeWarp.Nuru.Behavior\") // For TelemetryBehavior");
    sb.AppendLine("        .AddOtlpExporter(o => o.Endpoint = __endpoint)");
    sb.AppendLine("        .Build();");
    sb.AppendLine();
    sb.AppendLine("      app.LoggerFactory = global::Microsoft.Extensions.Logging.LoggerFactory.Create(builder =>");
    sb.AppendLine("      {");
    sb.AppendLine("        builder.SetMinimumLevel(global::Microsoft.Extensions.Logging.LogLevel.Debug);");
    sb.AppendLine("        builder.AddOpenTelemetry(options =>");
    sb.AppendLine("        {");
    sb.AppendLine("          options.SetResourceBuilder(__resource);");
    sb.AppendLine("          options.AddOtlpExporter(o => o.Endpoint = __endpoint);");
    sb.AppendLine("        });");
    sb.AppendLine("      });");
    sb.AppendLine();
    sb.AppendLine("      app.LoggerProvider = global::OpenTelemetry.Sdk.CreateLoggerProviderBuilder()");
    sb.AppendLine("        .SetResourceBuilder(__resource)");
    sb.AppendLine("        .AddOtlpExporter(o => o.Endpoint = __endpoint)");
    sb.AppendLine("        .Build();");
    sb.AppendLine("    }");
    sb.AppendLine();
  }

  /// <summary>
  /// Emits code to start a telemetry span and stopwatch before route execution.
  /// </summary>
  public static void EmitTelemetryStart(StringBuilder sb, string routePattern)
  {
    string escapedPattern = routePattern.Replace("\"", "\\\"", StringComparison.Ordinal);
    sb.AppendLine($"      using global::System.Diagnostics.Activity? __activity = __activitySource.StartActivity(\"{escapedPattern}\", global::System.Diagnostics.ActivityKind.Internal);");
    sb.AppendLine($"      __activity?.SetTag(\"command.pattern\", \"{escapedPattern}\");");
    sb.AppendLine("      global::System.Diagnostics.Stopwatch __sw = global::System.Diagnostics.Stopwatch.StartNew();");
    sb.AppendLine("      try");
    sb.AppendLine("      {");
  }

  /// <summary>
  /// Emits code to record successful telemetry after route execution.
  /// </summary>
  public static void EmitTelemetrySuccess(StringBuilder sb, string routePattern)
  {
    string escapedPattern = routePattern.Replace("\"", "\\\"", StringComparison.Ordinal);
    sb.AppendLine("        __sw.Stop();");
    sb.AppendLine("        __activity?.SetStatus(global::System.Diagnostics.ActivityStatusCode.Ok);");
    sb.AppendLine($"        __commandsInvoked.Add(1, new global::System.Collections.Generic.KeyValuePair<string, object?>(\"command\", \"{escapedPattern}\"));");
    sb.AppendLine($"        __commandDuration.Record(__sw.ElapsedMilliseconds, new global::System.Collections.Generic.KeyValuePair<string, object?>(\"command\", \"{escapedPattern}\"));");
  }

  /// <summary>
  /// Emits the catch block for recording telemetry errors.
  /// </summary>
  public static void EmitTelemetryCatch(StringBuilder sb, string routePattern)
  {
    string escapedPattern = routePattern.Replace("\"", "\\\"", StringComparison.Ordinal);
    sb.AppendLine("      }");
    sb.AppendLine("      catch (global::System.Exception __telemetryEx)");
    sb.AppendLine("      {");
    sb.AppendLine("        __sw.Stop();");
    sb.AppendLine("        __activity?.SetStatus(global::System.Diagnostics.ActivityStatusCode.Error, __telemetryEx.Message);");
    sb.AppendLine("        __activity?.SetTag(\"error.type\", __telemetryEx.GetType().Name);");
    sb.AppendLine("        __activity?.SetTag(\"error.message\", __telemetryEx.Message);");
    sb.AppendLine("        __commandsErrored.Add(1,");
    sb.AppendLine($"          new global::System.Collections.Generic.KeyValuePair<string, object?>(\"command\", \"{escapedPattern}\"),");
    sb.AppendLine("          new global::System.Collections.Generic.KeyValuePair<string, object?>(\"error.type\", __telemetryEx.GetType().Name));");
    sb.AppendLine("        __commandDuration.Record(__sw.ElapsedMilliseconds,");
    sb.AppendLine($"          new global::System.Collections.Generic.KeyValuePair<string, object?>(\"command\", \"{escapedPattern}\"),");
    sb.AppendLine("          new global::System.Collections.Generic.KeyValuePair<string, object?>(\"status\", \"error\"));");
    sb.AppendLine("        throw;");
    sb.AppendLine("      }");
  }

  /// <summary>
  /// Emits code to flush telemetry before returning from RunAsync.
  /// </summary>
  public static void EmitTelemetryFlush(StringBuilder sb)
  {
    sb.AppendLine("    // Flush telemetry before exit (critical for CLI apps)");
    sb.AppendLine("    await app.FlushTelemetryAsync().ConfigureAwait(false);");
  }
}
