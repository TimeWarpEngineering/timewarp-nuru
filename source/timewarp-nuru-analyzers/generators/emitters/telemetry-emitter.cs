// Emitter that generates OpenTelemetry infrastructure when UseTelemetry() is called.
//
// When telemetry is enabled, this emitter generates:
// - ActivitySource and Meter static fields
// - TracerProvider and MeterProvider setup code
//
// Command spans and command metrics are not emitted here. UseTelemetry() registers
// TelemetryBehavior as the app's outermost behavior (IrAppBuilder.EffectiveBehaviors, task 552),
// and the setup below listens to its "TimeWarp.Nuru.Behavior" source and meter.
//
// Extra sources and meters an app names through NuruTelemetryOptions.ActivitySources / Meters
// (task 561, e.g. "TimeWarp.Taratibu" walk spans) are added after Nuru's own, one literal
// AddSource / AddMeter call per name. With none configured the output is unchanged.

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
  public static void EmitTelemetryFields(StringBuilder sb, bool enableTracing, bool enableMetrics)
  {
    if (!enableTracing && !enableMetrics)
      return;

    sb.AppendLine("  // ═══════════════════════════════════════════════════════════════════════════════");
    sb.AppendLine("  // TELEMETRY INFRASTRUCTURE (generated because UseTelemetry() was called)");
    sb.AppendLine("  // ═══════════════════════════════════════════════════════════════════════════════");
    sb.AppendLine();
    if (enableTracing)
    {
      sb.AppendLine("  private static readonly global::System.Diagnostics.ActivitySource __activitySource = new(\"TimeWarp.Nuru\", \"1.0.0\");");
    }

    if (enableMetrics)
    {
      sb.AppendLine("  private static readonly global::System.Diagnostics.Metrics.Meter __meter = new(\"TimeWarp.Nuru\", \"1.0.0\");");
    }

    sb.AppendLine();
  }

  /// <summary>
  /// Emits the telemetry provider setup code.
  /// This should be called at the start of RunAsync_Intercepted to set up OTLP exporters.
  /// </summary>
  public static void EmitTelemetrySetup(StringBuilder sb, TelemetryModel options)
  {
    if (!options.EnableTracing && !options.EnableMetrics && !options.EnableLogging)
    {
      return;
    }

    sb.AppendLine("    // Setup telemetry providers if an OTLP endpoint is configured");
    if (!string.IsNullOrEmpty(options.OtlpEndpoint))
    {
      sb.AppendLine($"    string? __otlpEndpoint = \"{EmitterStringUtils.EscapeForStringLiteral(options.OtlpEndpoint)}\";");
    }
    else
    {
      sb.AppendLine("    string? __otlpEndpoint = global::System.Environment.GetEnvironmentVariable(\"OTEL_EXPORTER_OTLP_ENDPOINT\");");
    }

    sb.AppendLine("    if (!string.IsNullOrEmpty(__otlpEndpoint))");
    sb.AppendLine("    {");

    if (!string.IsNullOrEmpty(options.ServiceName))
    {
      sb.AppendLine("      string __serviceName = global::System.Environment.GetEnvironmentVariable(\"OTEL_SERVICE_NAME\")");
      sb.AppendLine($"        ?? \"{EmitterStringUtils.EscapeForStringLiteral(options.ServiceName)}\";");
    }
    else
    {
      sb.AppendLine("      string __serviceName = global::System.Environment.GetEnvironmentVariable(\"OTEL_SERVICE_NAME\")");
      sb.AppendLine("        ?? global::System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name");
      sb.AppendLine("        ?? \"nuru-app\";");
    }

    if (!string.IsNullOrEmpty(options.ServiceVersion))
    {
      sb.AppendLine("      string? __serviceVersion = global::System.Environment.GetEnvironmentVariable(\"OTEL_SERVICE_VERSION\")");
      sb.AppendLine($"        ?? \"{EmitterStringUtils.EscapeForStringLiteral(options.ServiceVersion)}\";");
    }
    else
    {
      sb.AppendLine("      string? __serviceVersion = global::System.Environment.GetEnvironmentVariable(\"OTEL_SERVICE_VERSION\")");
      sb.AppendLine("        ?? global::System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version?.ToString();");
    }

    sb.AppendLine();
    // Extension methods are called in static form with global:: declaring types: the generated
    // file cannot rely on the app's usings to bring them into scope (task 521).
    sb.AppendLine("      global::OpenTelemetry.Resources.ResourceBuilder __resource = global::OpenTelemetry.Resources.ResourceBuilderExtensions.AddService(");
    sb.AppendLine("        global::OpenTelemetry.Resources.ResourceBuilder.CreateDefault(), serviceName: __serviceName, serviceVersion: __serviceVersion);");
    sb.AppendLine();
    // OTLP endpoint (task 551). Exporters never get o.Endpoint = <base> directly: that turns off
    // the SDK's /v1/{signal} append for http/protobuf. NuruOtlpEndpoint.Configure sets the
    // per-signal URL from a code-set OtlpEndpoint (a base URL) or from
    // OTEL_EXPORTER_OTLP_{SIGNAL}_ENDPOINT, and otherwise leaves the SDK's env handling on.
    if (!string.IsNullOrEmpty(options.OtlpEndpoint))
    {
      sb.AppendLine("      string? __otlpCodeEndpoint = __otlpEndpoint;");
    }
    else
    {
      sb.AppendLine("      string? __otlpCodeEndpoint = null;");
    }

    sb.AppendLine();

    if (options.EnableTracing)
    {
      sb.AppendLine("      global::OpenTelemetry.Trace.TracerProviderBuilder __tracerBuilder = global::OpenTelemetry.Sdk.CreateTracerProviderBuilder();");
      sb.AppendLine("      __tracerBuilder = global::OpenTelemetry.Trace.TracerProviderBuilderExtensions.SetResourceBuilder(__tracerBuilder, __resource);");
      sb.AppendLine("      __tracerBuilder = __tracerBuilder.AddSource(__activitySource.Name);");
      sb.AppendLine("      __tracerBuilder = __tracerBuilder.AddSource(\"TimeWarp.Nuru.Behavior\"); // Command spans (TelemetryBehavior)");
      foreach (string source in options.ActivitySources)
      {
        sb.AppendLine($"      __tracerBuilder = __tracerBuilder.AddSource(\"{EmitterStringUtils.EscapeForStringLiteral(source)}\");");
      }

      sb.AppendLine($"      __tracerBuilder = global::OpenTelemetry.Trace.OtlpTraceExporterHelperExtensions.AddOtlpExporter(__tracerBuilder, {ConfigureExporter("Traces")});");
      sb.AppendLine("      app.TracerProvider = global::OpenTelemetry.Trace.TracerProviderBuilderExtensions.Build(__tracerBuilder);");
      sb.AppendLine();
    }

    if (options.EnableMetrics)
    {
      sb.AppendLine("      global::OpenTelemetry.Metrics.MeterProviderBuilder __meterBuilder = global::OpenTelemetry.Sdk.CreateMeterProviderBuilder();");
      sb.AppendLine("      __meterBuilder = global::OpenTelemetry.Metrics.MeterProviderBuilderExtensions.SetResourceBuilder(__meterBuilder, __resource);");
      sb.AppendLine("      __meterBuilder = __meterBuilder.AddMeter(__meter.Name);");
      sb.AppendLine("      __meterBuilder = __meterBuilder.AddMeter(\"TimeWarp.Nuru.Behavior\"); // Command metrics (TelemetryBehavior)");
      foreach (string meter in options.Meters)
      {
        sb.AppendLine($"      __meterBuilder = __meterBuilder.AddMeter(\"{EmitterStringUtils.EscapeForStringLiteral(meter)}\");");
      }

      sb.AppendLine($"      __meterBuilder = global::OpenTelemetry.Metrics.OtlpMetricExporterExtensions.AddOtlpExporter(__meterBuilder, {ConfigureExporter("Metrics")});");
      sb.AppendLine("      app.MeterProvider = global::OpenTelemetry.Metrics.MeterProviderBuilderExtensions.Build(__meterBuilder);");
      sb.AppendLine();
    }

    if (options.EnableLogging)
    {
      sb.AppendLine("      // Logging: Export structured logs to OTLP");
      sb.AppendLine("      app.LoggerFactory = global::Microsoft.Extensions.Logging.LoggerFactory.Create(builder =>");
      sb.AppendLine("      {");
      // SetMinimumLevel cannot be called in static form: Microsoft.Extensions.Logging.LoggingBuilderExtensions
      // is declared in both Microsoft.Extensions.Logging and Microsoft.Extensions.Logging.Configuration
      // (CS0433). Configure LoggerFilterOptions.MinLevel directly, which is what SetMinimumLevel does.
      sb.AppendLine("        global::Microsoft.Extensions.DependencyInjection.OptionsServiceCollectionExtensions.Configure<global::Microsoft.Extensions.Logging.LoggerFilterOptions>(");
      sb.AppendLine("          builder.Services, o => o.MinLevel = global::Microsoft.Extensions.Logging.LogLevel.Information);");
      sb.AppendLine("        global::Microsoft.Extensions.Logging.OpenTelemetryLoggingExtensions.AddOpenTelemetry(builder, options =>");
      sb.AppendLine("        {");
      sb.AppendLine("          options.SetResourceBuilder(__resource);");
      sb.AppendLine($"          global::OpenTelemetry.Logs.OtlpLogExporterHelperExtensions.AddOtlpExporter(options, {ConfigureExporter("Logs")});");
      sb.AppendLine("        });");
      sb.AppendLine("      });");
    }

    sb.AppendLine("    }");
    sb.AppendLine();
  }

  /// <summary>
  /// Returns the AddOtlpExporter configure delegate for one signal.
  /// </summary>
  /// <param name="signalConstant">The NuruOtlpEndpoint constant name: Traces, Metrics, or Logs.</param>
  private static string ConfigureExporter(string signalConstant) =>
    "o => global::TimeWarp.Nuru.NuruOtlpEndpoint.Configure(o, __otlpCodeEndpoint, "
    + $"global::TimeWarp.Nuru.NuruOtlpEndpoint.{signalConstant})";

  /// <summary>
  /// Emits code to flush telemetry before returning from RunAsync.
  /// </summary>
  public static void EmitTelemetryFlush(StringBuilder sb)
  {
    sb.AppendLine("    // Flush telemetry before exit (critical for CLI apps)");
    sb.AppendLine("    await app.FlushTelemetryAsync().ConfigureAwait(false);");
  }
}
