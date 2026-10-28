namespace TimeWarp.Nuru;

using OpenTelemetry.Exporter;

#region Purpose
// Pick the OTLP export URL and protocol for one signal (traces, metrics, logs).
// Generated UseTelemetry() setup calls Configure inside each AddOtlpExporter delegate.
#endregion

#region Design
// The SDK's per-signal AddOtlpExporter builds its options from the base env vars only
// (OTEL_EXPORTER_OTLP_ENDPOINT / _PROTOCOL). It appends /v1/{signal} for http/protobuf only
// when the endpoint came from that env var: setting Endpoint in code turns the append off,
// so a base URL posted to "/" and the collector returned 404 (task 551). It also ignores
// the per-signal OTEL_EXPORTER_OTLP_{SIGNAL}_ENDPOINT / _PROTOCOL vars; only UseOtlpExporter
// reads those.
//
// Configure applies the OTel spec order:
// 1. A code-set NuruTelemetryOptions.OtlpEndpoint wins. It is a base URL, like the env var:
//    for http/protobuf, ForSignal appends v1/{signal} unless the path already ends with it.
// 2. Otherwise OTEL_EXPORTER_OTLP_{SIGNAL}_ENDPOINT is used as-is, per the spec.
// 3. Otherwise Endpoint is left alone, so the SDK's own base-env handling (with the append)
//    stays on.
// A set OTEL_EXPORTER_OTLP_{SIGNAL}_PROTOCOL overrides the protocol for that signal.
// gRPC endpoints pass through unchanged: the SDK always appends the gRPC service path.
#endregion

/// <summary>
/// Resolves the OTLP export endpoint and protocol for one signal.
/// Used by generated <c>UseTelemetry()</c> setup.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class NuruOtlpEndpoint
{
  /// <summary>Signal name for traces.</summary>
  public const string Traces = "traces";

  /// <summary>Signal name for metrics.</summary>
  public const string Metrics = "metrics";

  /// <summary>Signal name for logs.</summary>
  public const string Logs = "logs";

  /// <summary>
  /// Applies the endpoint and protocol for <paramref name="signal"/> to SDK exporter options.
  /// </summary>
  /// <param name="options">Options the SDK already filled from the base <c>OTEL_EXPORTER_OTLP_*</c> env vars.</param>
  /// <param name="codeEndpoint"><see cref="NuruTelemetryOptions.OtlpEndpoint"/>, or null when not set in code.</param>
  /// <param name="signal"><see cref="Traces"/>, <see cref="Metrics"/>, or <see cref="Logs"/>.</param>
  public static void Configure(OtlpExporterOptions options, string? codeEndpoint, string signal)
  {
    ArgumentNullException.ThrowIfNull(options);
    ArgumentException.ThrowIfNullOrEmpty(signal);

    string signalUpper = signal.ToUpperInvariant();

    string? signalProtocol = Environment.GetEnvironmentVariable($"OTEL_EXPORTER_OTLP_{signalUpper}_PROTOCOL");
    if (TryParseProtocol(signalProtocol, out OtlpExportProtocol protocol))
      options.Protocol = protocol;

    if (!string.IsNullOrEmpty(codeEndpoint))
    {
      options.Endpoint = ForSignal(new Uri(codeEndpoint), options.Protocol, signal);
      return;
    }

    string? signalEndpoint = Environment.GetEnvironmentVariable($"OTEL_EXPORTER_OTLP_{signalUpper}_ENDPOINT");
    if (!string.IsNullOrEmpty(signalEndpoint) && Uri.TryCreate(signalEndpoint, UriKind.Absolute, out Uri? signalUri))
      options.Endpoint = signalUri;
  }

  /// <summary>
  /// Returns the URL the exporter should post <paramref name="signal"/> to, given a base URL.
  /// </summary>
  /// <param name="baseEndpoint">The collector base URL, such as <c>https://collector:4318/</c>.</param>
  /// <param name="protocol">The exporter protocol.</param>
  /// <param name="signal"><see cref="Traces"/>, <see cref="Metrics"/>, or <see cref="Logs"/>.</param>
  /// <returns>
  /// For <see cref="OtlpExportProtocol.HttpProtobuf"/>, <paramref name="baseEndpoint"/> with
  /// <c>v1/{signal}</c> appended unless its path already ends with it. Otherwise
  /// <paramref name="baseEndpoint"/> unchanged.
  /// </returns>
  public static Uri ForSignal(Uri baseEndpoint, OtlpExportProtocol protocol, string signal)
  {
    ArgumentNullException.ThrowIfNull(baseEndpoint);
    ArgumentException.ThrowIfNullOrEmpty(signal);

    if (protocol != OtlpExportProtocol.HttpProtobuf)
      return baseEndpoint;

    string signalPath = $"/v1/{signal}";
    string trimmed = baseEndpoint.AbsoluteUri.TrimEnd('/');
    if (trimmed.EndsWith(signalPath, StringComparison.OrdinalIgnoreCase))
      return baseEndpoint;

    return new Uri(trimmed + signalPath);
  }

  private static bool TryParseProtocol(string? value, out OtlpExportProtocol protocol)
  {
    switch (value?.Trim().ToUpperInvariant())
    {
      case "GRPC":
        protocol = OtlpExportProtocol.Grpc;
        return true;
      case "HTTP/PROTOBUF":
        protocol = OtlpExportProtocol.HttpProtobuf;
        return true;
      default:
        protocol = default;
        return false;
    }
  }
}
