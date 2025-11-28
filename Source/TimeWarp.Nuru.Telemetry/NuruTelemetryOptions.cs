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
  /// OTLP endpoint for telemetry export.
  /// If not set, reads from OTEL_EXPORTER_OTLP_ENDPOINT environment variable.
  /// If neither is set, telemetry export is disabled.
  /// </summary>
  public string? OtlpEndpoint { get; set; }

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
