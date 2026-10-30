namespace TimeWarp.Nuru;

/// <summary>
/// Configuration options for Nuru OpenTelemetry integration.
/// </summary>
public sealed class NuruTelemetryOptions
{
  /// <summary>
  /// Service name for telemetry identification.
  /// Defaults to the entry assembly name or "nuru-app".
  /// Can be overridden via OTEL_SERVICE_NAME environment variable.
  /// </summary>
  public string ServiceName { get; set; } = GetDefaultServiceName();

  /// <summary>
  /// Service version for telemetry identification.
  /// Defaults to the entry assembly version.
  /// Can be overridden via OTEL_SERVICE_VERSION environment variable.
  /// </summary>
  public string? ServiceVersion { get; set; } = GetDefaultServiceVersion();

  /// <summary>
  /// Enable tracing (Activity spans) for command execution.
  /// Default: true
  /// </summary>
  public bool EnableTracing { get; set; } = true;

  /// <summary>
  /// Enable metrics collection for command statistics.
  /// Default: true
  /// </summary>
  public bool EnableMetrics { get; set; } = true;

  /// <summary>
  /// Enable OpenTelemetry logging provider.
  /// Default: true
  /// </summary>
  public bool EnableLogging { get; set; } = true;

  /// <summary>
  /// OTLP collector base URL for telemetry export.
  /// If not set, reads from OTEL_EXPORTER_OTLP_ENDPOINT environment variable.
  /// If neither is set, telemetry export is disabled.
  /// </summary>
  /// <remarks>
  /// This is a base URL, with the same meaning as OTEL_EXPORTER_OTLP_ENDPOINT. With
  /// <c>OTEL_EXPORTER_OTLP_PROTOCOL=http/protobuf</c>, traces, metrics, and logs post to
  /// <c>{base}/v1/traces</c>, <c>{base}/v1/metrics</c>, and <c>{base}/v1/logs</c>. A path that
  /// already ends with the signal path is kept. With gRPC the URL is used unchanged.
  /// A value set here takes precedence over every OTEL_EXPORTER_OTLP_*_ENDPOINT variable.
  /// When it is not set, OTEL_EXPORTER_OTLP_{TRACES,METRICS,LOGS}_ENDPOINT override the base
  /// variable for their signal and are used as-is.
  /// <para>
  /// OTLP sinks receive process telemetry and must be treated as a trust boundary.
  /// Point this only at collectors you control or otherwise trust.
  /// </para>
  /// </remarks>
  public string? OtlpEndpoint { get; set; }

  /// <summary>
  /// Extra <see cref="System.Diagnostics.ActivitySource"/> names the tracer provider exports,
  /// in addition to Nuru's own <c>TimeWarp.Nuru</c> and <c>TimeWarp.Nuru.Behavior</c>.
  /// </summary>
  /// <remarks>
  /// Use this for spans a library creates on its own source, for example
  /// <c>options.ActivitySources.Add("TimeWarp.Taratibu")</c> for Taratibu walk spans.
  /// The source generator reads each <c>Add</c> call at compile time, so the name must be a
  /// string literal or a <c>const</c>; other calls are ignored. OpenTelemetry wildcards such as
  /// <c>"MyCompany.*"</c> are passed through unchanged.
  /// </remarks>
  public IList<string> ActivitySources { get; } = [];

  /// <summary>
  /// Extra <see cref="System.Diagnostics.Metrics.Meter"/> names the meter provider exports,
  /// in addition to Nuru's own <c>TimeWarp.Nuru</c> and <c>TimeWarp.Nuru.Behavior</c>.
  /// </summary>
  /// <remarks>
  /// Read at compile time like <see cref="ActivitySources"/>: each <c>Add</c> argument must be a
  /// string literal or a <c>const</c>.
  /// </remarks>
  public IList<string> Meters { get; } = [];

  /// <summary>
  /// Gets the effective OTLP endpoint, considering both the property and environment variable.
  /// </summary>
  internal string? EffectiveOtlpEndpoint =>
    OtlpEndpoint ?? Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");

  /// <summary>
  /// Gets the effective service name, considering both the property and environment variable.
  /// </summary>
  internal string EffectiveServiceName =>
    Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME") ?? ServiceName;

  /// <summary>
  /// Determines if telemetry export should be enabled based on configuration.
  /// </summary>
  internal bool ShouldExportTelemetry => !string.IsNullOrEmpty(EffectiveOtlpEndpoint);

  private static string GetDefaultServiceName()
  {
    return System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name ?? "nuru-app";
  }

  private static string? GetDefaultServiceVersion()
  {
    return System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version?.ToString();
  }
}
