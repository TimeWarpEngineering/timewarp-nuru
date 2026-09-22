namespace TimeWarp.Nuru.Generators;

#region Purpose
// Compile-time snapshot of NuruTelemetryOptions extracted from UseTelemetry(Action<>).
#endregion

#region Design
// Null ServiceName / ServiceVersion / OtlpEndpoint mean "use the generated fallback"
// (assembly name, assembly version, OTEL_EXPORTER_OTLP_ENDPOINT). Enable* flags default
// true to match NuruTelemetryOptions. The generator emits property-then-env for the
// OTLP endpoint and env-then-property for service name, matching the runtime type.
#endregion

/// <summary>
/// Configuration options for OpenTelemetry emission.
/// </summary>
/// <param name="ServiceName">Explicit service name, or null to use assembly / env fallback.</param>
/// <param name="ServiceVersion">Explicit service version, or null to use assembly / env fallback.</param>
/// <param name="EnableTracing">Whether to emit TracerProvider setup.</param>
/// <param name="EnableMetrics">Whether to emit MeterProvider setup.</param>
/// <param name="EnableLogging">Whether to emit OpenTelemetry logging provider setup.</param>
/// <param name="OtlpEndpoint">Explicit OTLP endpoint, or null to read OTEL_EXPORTER_OTLP_ENDPOINT.</param>
public sealed record TelemetryModel(
  string? ServiceName,
  string? ServiceVersion,
  bool EnableTracing,
  bool EnableMetrics,
  bool EnableLogging,
  string? OtlpEndpoint)
{
  /// <summary>
  /// Default telemetry configuration matching <c>NuruTelemetryOptions</c> defaults.
  /// </summary>
  public static readonly TelemetryModel Default = new(
    ServiceName: null,
    ServiceVersion: null,
    EnableTracing: true,
    EnableMetrics: true,
    EnableLogging: true,
    OtlpEndpoint: null);
}
