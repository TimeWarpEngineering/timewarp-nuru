#region Purpose
// Descriptors for NURU_T telemetry diagnostics, reported when the generator cannot read a UseTelemetry option.
#endregion

#region Design
// The generator reads UseTelemetry options at compile time and emits them as literals (task 533).
// A value it cannot read used to be skipped in silence, so an OtlpEndpoint from a variable exported
// nothing and said nothing. These are warnings, not errors: the app still builds and runs with the
// fallback the message names.
#endregion

namespace TimeWarp.Nuru;

/// <summary>
/// Diagnostic descriptors for UseTelemetry options the source generator cannot read.
/// </summary>
internal static partial class DiagnosticDescriptors
{
  internal const string TelemetryCategory = "Telemetry";

  public static readonly DiagnosticDescriptor TelemetryOptionNotConstant = new(
      id: "NURU_T001",
      title: "UseTelemetry option value is not a compile-time constant",
      messageFormat: "UseTelemetry ignores this value for '{0}' because it is not a compile-time constant. Use a literal or a const. {1}.",
      category: TelemetryCategory,
      defaultSeverity: DiagnosticSeverity.Warning,
      isEnabledByDefault: true,
      description: "The source generator reads the UseTelemetry configure lambda at compile time and emits each option as a literal, so nothing is resolved at run time (AOT and trim safe). A value it cannot evaluate as a constant, such as a local variable, a method call, or a non-const field or property, is ignored. For OtlpEndpoint the app then reads OTEL_EXPORTER_OTLP_ENDPOINT; set that variable to choose the endpoint at run time, or assign a string literal or a const.");

  public static readonly DiagnosticDescriptor TelemetryConfigureNotLambda = new(
      id: "NURU_T002",
      title: "UseTelemetry configure argument is not a lambda",
      messageFormat: "UseTelemetry ignores this configure delegate because it is not an inline lambda. The default telemetry options apply, and OtlpEndpoint is read from OTEL_EXPORTER_OTLP_ENDPOINT.",
      category: TelemetryCategory,
      defaultSeverity: DiagnosticSeverity.Warning,
      isEnabledByDefault: true,
      description: "The source generator reads UseTelemetry options only from an inline lambda, such as UseTelemetry(options => options.ServiceName = \"my-cli\"). A method group or a delegate variable is never run, so none of the options it sets apply. Write the assignments in a lambda passed to UseTelemetry.");
}
